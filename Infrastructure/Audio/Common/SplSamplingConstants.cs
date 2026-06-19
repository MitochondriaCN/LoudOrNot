namespace LoudOrNot.Infrastructure.Audio.Common;

internal static class SplSamplingConstants
{
    public const int SampleRate = 44100;
    public const int Channels = 1;
    public const int BitsPerSample = 16;
    public const int BytesPerSample = BitsPerSample / 8;
    public const int SampleDurationMilliseconds = 250;

    public const int SampleFrameCount = SampleRate * SampleDurationMilliseconds / 1000;
    public const int SampleByteCount = SampleFrameCount * Channels * BytesPerSample;
}
