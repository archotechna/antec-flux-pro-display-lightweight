using System.ComponentModel;
using HidLibrary;
using Microsoft.Win32;

namespace FluxProDisplay;

public partial class FluxProDisplayTray : Form
{
    public HardwareMonitor Monitor;
    public bool ConnectionStatus = false;
    public ToolStripLabel? ConnectionStatusLabel;
    public ToolStripMenuItem? StartupToggleMenuItem;
    public string AppName = "FluxProDisplay";

    // offload to appsettings
    private const int PollingInterval = 1;
    private const int VendorId = 0x2022;
    private const int ProductId = 0x0522;

    // other UI components for the tab
    private NotifyIcon _appStatusNotifyIcon = null!;
    private Container _component = null!;
    private ContextMenuStrip _contextMenuStrip = null!;

    public FluxProDisplayTray()
    {
        InitializeComponent();

        Monitor = new HardwareMonitor();

        SetUpTrayIcon();

        _ = WriteToDisplay();
    }

    private void SetUpTrayIcon()
    {
        _component = new Container();
        _appStatusNotifyIcon = new NotifyIcon(_component);
        _appStatusNotifyIcon.Visible = true;

        _contextMenuStrip = new ContextMenuStrip();

        var appNameLabel = new ToolStripLabel("Antec Flux Pro Display Service");
        appNameLabel.ForeColor = Color.Gray;
        appNameLabel.Enabled = false;
        _contextMenuStrip.Items.Add(appNameLabel);

        _contextMenuStrip.Items.Add(new ToolStripSeparator());

        ConnectionStatusLabel = new ToolStripLabel("Not Connected");
        ConnectionStatusLabel.ForeColor = Color.Crimson;
        ConnectionStatusLabel.Enabled = true;
        _contextMenuStrip.Items.Add(ConnectionStatusLabel);

        // menu items
        StartupToggleMenuItem = new ToolStripMenuItem();
        StartupToggleMenuItem.Click += StartupToggleMenuItemClicked;

        var quitMenuItem = new ToolStripMenuItem("Quit");
        quitMenuItem.Click += QuitMenuItem_Click!;

        // separator to separate
        _contextMenuStrip.Items.Add(new ToolStripSeparator());
        _contextMenuStrip.Items.Add(StartupToggleMenuItem);
        _contextMenuStrip.Items.Add(quitMenuItem);

        _appStatusNotifyIcon.ContextMenuStrip = _contextMenuStrip;

        UpdateStartupMenuItemText();

        _appStatusNotifyIcon.Icon = new Icon("assets/sevensegment2.ico");
    }

    private void StartupToggleMenuItemClicked(object? sender, EventArgs e)
    {
        var exePath = Application.ExecutablePath;

        using (var key = Registry.CurrentUser.OpenSubKey(
                   @"Software\Microsoft\Windows\CurrentVersion\Run", true))
        {
            bool isEnabled = key?.GetValue(AppName) != null;

            if (isEnabled)
            {
                key?.DeleteValue(AppName);
            }
            else
            {
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
        }

        UpdateStartupMenuItemText();
    }

    private void UpdateStartupMenuItemText()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(
                   @"Software\Microsoft\Windows\CurrentVersion\Run", false))
        {
            var isEnabled = key?.GetValue(AppName) != null;
            StartupToggleMenuItem!.Text = isEnabled ? "✓ Start with Windows" : "Start with Windows";
        }
    }

    private void QuitMenuItem_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    /// <summary>
    /// Hides the main window on startup.
    /// </summary>
    /// <param name="value"></param>
    protected override void SetVisibleCore(bool value)
    {
        if (!IsHandleCreated) {
            value = false;
            CreateHandle();
        }
        base.SetVisibleCore(value);
    }

    private async Task WriteToDisplay()
    {
        // interval of 1 sec
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(PollingInterval));

        var device = HidDevices.Enumerate(VendorId, ProductId).FirstOrDefault();

        if (device != null)
        {
            ConnectionStatusLabel!.Text = "Connected";
            ConnectionStatusLabel.ForeColor = Color.Green;
        }

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
        var reportLength = device.Capabilities.OutputReportByteLength;
        var payload = new byte[reportLength];

        // reporting number, and other information needed to send to the display
        payload[0] = 0;
        payload[1] = 85;
        payload[2] = 170;
        payload[3] = 1;
        payload[4] = 1;
        payload[5] = 6;

        return FormatDisplayPayload(payload, Monitor.GetCpuTemperature(), Monitor.GetGpuTemperature());
    }

    /// <summary>
    /// formats the payload correctly to send information to the antec flux pro display.
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="cpuTemperature"></param>
    /// <param name="gpuTemperature"></param>
    /// <returns></returns>
    private byte[] FormatDisplayPayload(byte[] payload, float? cpuTemperature, float? gpuTemperature)
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
        var checksum = payload.Aggregate<byte, byte>(0, (current, b) => (byte)(current + b));
        payload[12] = checksum;

        return payload;
    }
}