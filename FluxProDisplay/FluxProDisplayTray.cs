using System;
using HidLibrary;

namespace FluxProDisplay;

public partial class FluxProDisplayTray : Form
{
    public HidDevice? Device;
    public HardwareMonitor Monitor;

    // offload to appsettings
    private const int PollingInterval = 1;
    const int VendorId = 0x2022;
    const int ProductId = 0x0522;

    public FluxProDisplayTray()
    {
        InitializeComponent();

        Monitor = new HardwareMonitor();
        _ = WriteToDisplay();
    }

    private async Task WriteToDisplay()
    {
        // interval of 1 sec
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(PollingInterval));

        var device = HidDevices.Enumerate(VendorId, ProductId).FirstOrDefault();

        do
        {
            device?.Write(GeneratePayload(device));
        } while (await timer.WaitForNextTickAsync());
    }

    /// <summary>
    /// generates the encoded payload to the display
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    private byte[] GeneratePayload(HidDevice device)
    {
        int reportLength = device.Capabilities.OutputReportByteLength;
        byte[] payload = new byte[reportLength];

        // reporting number, and other information needed to send to the display
        payload[0] = 0;
        payload[1] = 85;
        payload[2] = 170;
        payload[3] = 1;
        payload[4] = 1;
        payload[5] = 6;

        return FormattedPayload(payload, Monitor.GetCpuTemperature(), Monitor.GetGpuTemperature());
    }

    private byte[] FormattedPayload(byte[] payload, float? cpuTemperature, float? gpuTemperature)
    {
        var roundedCpuTemp = Math.Round(cpuTemperature ?? 0, 1);
        var roundedGpuTemp = Math.Round(gpuTemperature ?? 0, 1);

        var wholeNumCpuTemp = (int)roundedCpuTemp;
        var tensPlaceCpuTemp = wholeNumCpuTemp / 10;
        var onesPlaceCpuTemp = wholeNumCpuTemp % 10;
        var tenthsPlaceCpuTemp = (int)((roundedCpuTemp - wholeNumCpuTemp) * 10);

        var wholeNumGpuTemp = (int)roundedGpuTemp;
        var tensPlaceGpuTemp = wholeNumGpuTemp / 10;
        var onesPlaceGpuTemp = wholeNumGpuTemp % 10;
        var tenthsPlaceGpuTemp = (int)((roundedGpuTemp - wholeNumGpuTemp) * 10);


        payload[6] = (byte)tensPlaceCpuTemp;
        payload[7] = (byte)onesPlaceCpuTemp;
        payload[8] = (byte)tenthsPlaceCpuTemp;

        payload[9] = (byte)tensPlaceGpuTemp;
        payload[10] = (byte)onesPlaceGpuTemp;
        payload[11] = (byte)tenthsPlaceGpuTemp;

        // generate checksum per item that is sent to the display
        byte checksum = payload.Aggregate<byte, byte>(0, (current, b) => (byte)(current + b));

        payload[12] = checksum;

        return payload;
    }
}