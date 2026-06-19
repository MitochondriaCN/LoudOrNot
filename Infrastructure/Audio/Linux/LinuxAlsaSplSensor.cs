using System.Runtime.InteropServices;
using LoudOrNot.Domain.Audio;
using LoudOrNot.Infrastructure.Audio.Common;

namespace LoudOrNot.Infrastructure.Audio.Linux;

public sealed class LinuxAlsaSplSensor(string deviceId, string deviceName) : ISplSensor
{
    private const int SndPcmStreamCapture = 1;
    private const int SndPcmFormatS16Le = 2;
    private const int SndPcmAccessRwInterleaved = 3;

    public string Name { get; } = $"{deviceName} ALSA SPL sensor";
    public string Description { get; } = "通过 ALSA 采集默认输入设备 PCM 数据并估算瞬时声压级。";

    public InstantaneousAmbientSpl MeasureInstantaneousAmbientSpl()
    {
        var handle = IntPtr.Zero;
        var openResult = snd_pcm_open(ref handle, ToAlsaDeviceName(deviceId), SndPcmStreamCapture, 0);
        ThrowIfAlsaError(openResult, "打开 ALSA 输入设备失败");

        try
        {
            var setParamsResult = snd_pcm_set_params(
                handle,
                SndPcmFormatS16Le,
                SndPcmAccessRwInterleaved,
                SplSamplingConstants.Channels,
                SplSamplingConstants.SampleRate,
                1,
                SplSamplingConstants.SampleDurationMilliseconds * 1000U);
            ThrowIfAlsaError(setParamsResult, "配置 ALSA 输入设备失败");

            var buffer = new byte[SplSamplingConstants.SampleByteCount];
            var framesRead = snd_pcm_readi(handle, buffer, new UIntPtr((uint)SplSamplingConstants.SampleFrameCount));

            if (framesRead < 0)
            {
                snd_pcm_recover(handle, framesRead, 0);
                ThrowIfAlsaError(framesRead, "读取 ALSA 输入采样失败");
            }

            var byteCount = checked((int)framesRead * SplSamplingConstants.Channels * SplSamplingConstants.BytesPerSample);
            return PcmSplEstimator.EstimateInt16LittleEndian(buffer.AsSpan(0, byteCount), this);
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                snd_pcm_close(handle);
            }
        }
    }

    private static string ToAlsaDeviceName(string deviceId)
    {
        return string.IsNullOrWhiteSpace(deviceId) || deviceId.Contains('.')
            ? "default"
            : deviceId;
    }

    private static void ThrowIfAlsaError(long result, string message)
    {
        if (result >= 0)
        {
            return;
        }

        var error = Marshal.PtrToStringAnsi(snd_strerror((int)result)) ?? $"ALSA error {result}";
        throw new InvalidOperationException($"{message}: {error}");
    }

    [DllImport("libasound.so.2")]
    private static extern int snd_pcm_open(
        ref IntPtr pcm,
        string name,
        int stream,
        int mode);

    [DllImport("libasound.so.2")]
    private static extern int snd_pcm_set_params(
        IntPtr pcm,
        int format,
        int access,
        int channels,
        int rate,
        int softResample,
        uint latency);

    [DllImport("libasound.so.2")]
    private static extern long snd_pcm_readi(
        IntPtr pcm,
        byte[] buffer,
        UIntPtr size);

    [DllImport("libasound.so.2")]
    private static extern int snd_pcm_recover(
        IntPtr pcm,
        long err,
        int silent);

    [DllImport("libasound.so.2")]
    private static extern int snd_pcm_close(IntPtr pcm);

    [DllImport("libasound.so.2")]
    private static extern IntPtr snd_strerror(int errnum);
}
