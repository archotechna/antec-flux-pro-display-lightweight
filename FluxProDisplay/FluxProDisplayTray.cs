using System.ComponentModel;
using System.Diagnostics;
using FluxProDisplay.DTOs.AppSettings;
using HidLibrary;
using Microsoft.Win32.TaskScheduler;
using Task = System.Threading.Tasks.Task;

namespace FluxProDisplay;

public partial class FluxProDisplayTray : Form
{
    private readonly HardwareMonitor _monitor;
    private ToolStripLabel? _connectionStatusLabel;
    private ToolStripMenuItem? _startupToggleMenuItem;
    private const string ElevatedTaskName = "FluxProDisplayElevatedTask";
    
    // app settings
    private readonly string _appName;
    private readonly string _version;
    private readonly int _pollingInterval;
    private readonly int _vendorId;
    private readonly int _productId;

    // other UI components for the tab
    private NotifyIcon _appStatusNotifyIcon = null!;
    private Container _component = null!;
    private ContextMenuStrip _contextMenuStrip = null!;

    public FluxProDisplayTray(RootConfig configuration)
    {
        // check if iUnity is running to prevent conflicts before doing anything else
        CheckForIUnity();
        
        InitializeComponent();
        
        _monitor = new HardwareMonitor();
        
        // initialize variables from config file for easier changing
        _appName = configuration.AppInfo.Info;
        _version = configuration.AppInfo.Version;
        _pollingInterval = configuration.AppSettings.PollingInterval;
        _vendorId = configuration.AppSettings.VendorIdInt;
        _productId = configuration.AppSettings.ProductIdInt;

        SetUpTrayIcon();

        _ = WriteToDisplay();
    }

    private static void CheckForIUnity()
    {
        var isRunning =
            Process.GetProcessesByName("iunity").Length > 0 ||
            Process.GetProcessesByName("AntecHardwareMonitorWindowsService").Length > 0;

        if (!isRunning) return;

        MessageBox.Show("iUnity is running, please end the iUnity program and its related processes from task manager and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Environment.Exit(0);
    }

    private void SetUpTrayIcon()
    {
        _component = new Container();
        _appStatusNotifyIcon = new NotifyIcon(_component);
        _appStatusNotifyIcon.Visible = true;

        _contextMenuStrip = new ContextMenuStrip();

        var appNameLabel = new ToolStripLabel(_appName + " " + _version);
        appNameLabel.ForeColor = Color.Gray;
        appNameLabel.Enabled = false;
        _contextMenuStrip.Items.Add(appNameLabel);

        _contextMenuStrip.Items.Add(new ToolStripSeparator());

        _connectionStatusLabel = new ToolStripLabel();
        _connectionStatusLabel.ForeColor = Color.Crimson;
        _connectionStatusLabel.Enabled = true;
        _contextMenuStrip.Items.Add(_connectionStatusLabel);

        // menu items
        _startupToggleMenuItem = new ToolStripMenuItem();
        _startupToggleMenuItem.Click += StartupToggleMenuItemClicked;

        var quitMenuItem = new ToolStripMenuItem("Quit");
        quitMenuItem.Click += QuitMenuItem_Click!;

        // separator to separate
        _contextMenuStrip.Items.Add(new ToolStripSeparator());
        _contextMenuStrip.Items.Add(_startupToggleMenuItem);
        _contextMenuStrip.Items.Add(quitMenuItem);

        _appStatusNotifyIcon.ContextMenuStrip = _contextMenuStrip;

        UpdateStartupMenuItemText();

        _appStatusNotifyIcon.Icon = new Icon("assets/icon_disconnected.ico");
    }

    private void StartupToggleMenuItemClicked(object? sender, EventArgs e)
    {
        var exePath = Application.ExecutablePath;

        using (var ts = new TaskService())
        {
            var task = ts.FindTask(ElevatedTaskName);

            if (task != null)
            {
                ts.RootFolder.DeleteTask(ElevatedTaskName);
            }
            else
            {
                var td = ts.NewTask();

                td.RegistrationInfo.Description = "Flux Pro Display Service Task with Admin Privileges";
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Principal.LogonType = TaskLogonType.InteractiveToken;

                td.Triggers.Add(new LogonTrigger());
                td.Actions.Add(new ExecAction(exePath, null, Path.GetDirectoryName(exePath)));

                ts.RootFolder.RegisterTaskDefinition(ElevatedTaskName, td);
            }
        }

        UpdateStartupMenuItemText();
    }

    private void UpdateStartupMenuItemText()
    {
        using var ts = new TaskService();
        var taskEnabled = ts.FindTask(ElevatedTaskName) != null;
        _startupToggleMenuItem!.Text = taskEnabled ? "✓ Start with Windows" : "Start with Windows";
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
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_pollingInterval));

        var device = HidDevices.Enumerate(_vendorId, _productId).FirstOrDefault();

        if (device != null)
        {
            _connectionStatusLabel!.Text = "Connected";
            _appStatusNotifyIcon.Icon = new Icon("assets/icon_connected.ico");
            _connectionStatusLabel.ForeColor = Color.Green;
        }
        else
        {
            _connectionStatusLabel!.Text = "Not Connected";
            _appStatusNotifyIcon.Icon = new Icon("assets/icon_disconnected.ico");
            _connectionStatusLabel.ForeColor = Color.Crimson;
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

        return FormatDisplayPayload(payload, _monitor.GetCpuTemperature(), _monitor.GetGpuTemperature());
    }

    /// <summary>
    /// formats the payload correctly to send information to the antec flux pro display.
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="cpuTemperature"></param>
    /// <param name="gpuTemperature"></param>
    /// <returns></returns>
    private static byte[] FormatDisplayPayload(byte[] payload, float? cpuTemperature, float? gpuTemperature)
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