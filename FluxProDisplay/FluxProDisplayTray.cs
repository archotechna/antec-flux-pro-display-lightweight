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

        // representation of numbers
        Console.WriteLine(Monitor.GetCpuTemperature());
        payload[6] = 2;
        payload[7] = 4;
        payload[8] = 0;
        payload[9] = 1;
        payload[10] = 6;
        payload[11] = 0;
        payload[12] = 20;

        return payload;
    }
}