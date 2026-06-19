using System.Buffers.Binary;
using LoudOrNot.Domain.Audio;

namespace LoudOrNot.Infrastructure.Audio.Common;

internal static class PcmSplEstimator
{
    private const double DefaultFullScaleSpl = 94.0;
    private const double SilenceFloorSpl = 0.0;

    public static InstantaneousAmbientSpl EstimateInt16LittleEndian(
        ReadOnlySpan<byte> buffer,
        ISplSensor sensor,
        double fullScaleSpl = DefaultFullScaleSpl)
    {
        if (buffer.Length < 2)
        {
            throw new InvalidOperationException("音频采样数据为空，无法计算声压级。");
        }

        var sampleCount = buffer.Length / 2;
        double squareSum = 0;

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = BinaryPrimitives.ReadInt16LittleEndian(buffer.Slice(i * 2, 2));
            var normalized = sample / 32768.0;
            squareSum += normalized * normalized;
        }

        var rms = Math.Sqrt(squareSum / sampleCount);
        var spl = rms <= 0
            ? SilenceFloorSpl
            : Math.Max(SilenceFloorSpl, fullScaleSpl + 20.0 * Math.Log10(rms));

        return new InstantaneousAmbientSpl(DateTime.Now, spl, sensor);
    }
}
