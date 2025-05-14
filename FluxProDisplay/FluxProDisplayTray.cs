using System;
using HidLibrary;

namespace FluxProDisplay;

public partial class FluxProDisplayTray : Form
{
    public HidDevice? Device;

    private const int PollingInterval = 1;

    const int vendorId = 0x2022;  // Replace with your device's VID
    const int productId = 0x0522; // Replace with your device's PID

    public FluxProDisplayTray()
    {
        InitializeComponent();

        _ = WriteToDisplay();
    }

    private async Task WriteToDisplay()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(PollingInterval));

        do
        {
            var device = HidDevices.Enumerate(vendorId, productId).FirstOrDefault();
            int reportLength = device.Capabilities.OutputReportByteLength;

            // Example payload (report)
            // The first byte is the Report ID (usually 0x00)
            // 85, 170, 1, 1, 6, 2, 4, 0, 1, 6, 0, 20
            byte[] payload = new byte[reportLength]; // Customize as needed

            payload[0] = 0;
            payload[1] = 85;
            payload[2] = 170;
            payload[3] = 1;
            payload[4] = 1;
            payload[5] = 6;
            payload[6] = 2;
            payload[7] = 4;
            payload[8] = 0;
            payload[9] = 1;
            payload[10] = 6;
            payload[11] = 0;
            payload[12] = 20;

            device.Write(payload);

            device.CloseDevice();
        } while (await timer.WaitForNextTickAsync());
    }
}