using LibreHardwareMonitor.Hardware;

namespace FluxProDisplay;

public class HardwareMonitor
{
    public readonly Computer Computer;

    public HardwareMonitor()
    {
        // change this in the future to be configurable.
        Computer = new Computer()
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true
        };

        Computer.Open();
        Computer.Accept(new UpdateVisitor());
    }

    public float? GetCpuTemperature()
    {

        foreach (var hardware in Computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Tctl") || sensor.Name.Contains("Tdie") 
                            || sensor.Name.Contains("Package") 
                            || sensor.Name.Contains("Core")
                        )
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
        foreach (var hardware in Computer.Hardware)
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