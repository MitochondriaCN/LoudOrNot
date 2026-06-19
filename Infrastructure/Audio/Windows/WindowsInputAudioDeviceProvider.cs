using LoudOrNot.Application.Audio;
using LoudOrNot.Domain.Audio;
using System.Runtime.InteropServices;

namespace LoudOrNot.Infrastructure.Audio.Windows;

public class WindowsInputAudioDeviceProvider : IInputAudioDeviceProvider
{
    private const string DefaultDeviceId = "default";
    private const int MmsysErrNoError = 0;

    public IReadOnlyList<IInputAudioDevice> GetInputDevices()
    {
        var deviceCount = waveInGetNumDevs();
        var devices = new List<IInputAudioDevice>((int)deviceCount);

        for (uint deviceIndex = 0; deviceIndex < deviceCount; deviceIndex++)
        {
            var result = waveInGetDevCaps(
                deviceIndex,
                out var caps,
                (uint)Marshal.SizeOf<WaveInCaps>());

            if (result == MmsysErrNoError)
            {
                devices.Add(new WindowsInputAudioDevice(
                    deviceIndex.ToString(),
                    caps.ProductName,
                    "Windows 输入设备",
                    "Windows"));
            }
        }

        return devices;
    }

    public IInputAudioDevice GetCurrentDefaultInputDevice()
    {
        return new WindowsInputAudioDevice(
            DefaultDeviceId,
            "System default input device",
            "Windows 当前默认输入设备",
            "Windows");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct WaveInCaps
    {
        public ushort ManufacturerId;
        public ushort ProductId;
        public uint DriverVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ProductName;

        public uint Formats;
        public ushort Channels;
        public ushort Reserved;
        public uint Support;
    }

    [DllImport("winmm.dll")]
    private static extern uint waveInGetNumDevs();

    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int waveInGetDevCaps(
        uint deviceId,
        out WaveInCaps caps,
        uint capsSize);
}
