using LibreHardwareMonitor.Hardware;

namespace FluxProDisplay;

public class HardwareMonitor
{
    private readonly Computer _computer;

    public HardwareMonitor()
    {
        _computer = new Computer()
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true
        };

        _computer.Open();
        _computer.Accept(new UpdateVisitor());
    }

    public float? GetCpuTemperature()
    {
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && (sensor.Name.Contains("Tctl/Tdie") || sensor.Name.Contains("CPU Package")))
                    {
                        return sensor.Value;
                    }
                }
            }
        }

        return null;
    }

    public float? GetGpuTemperature()
    {
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
            {
                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature &&
                        sensor.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
                    {
                        var test = sensor.Value;
                        return sensor.Value;
                    }
                }
            }
        }

        return null;
    }
}