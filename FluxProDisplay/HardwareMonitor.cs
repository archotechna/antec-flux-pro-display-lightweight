using LibreHardwareMonitor.Hardware;

namespace FluxProDisplay;

public class HardwareMonitor
{
    public Computer Computer;

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
        foreach (IHardware hardware in Computer.Hardware)
        {
            Console.WriteLine("Hardware: {0}", hardware.Name);

            foreach (IHardware subhardware in hardware.SubHardware)
            {
                Console.WriteLine("\tSubhardware: {0}", subhardware.Name);

                foreach (ISensor sensor in subhardware.Sensors)
                {
                    Console.WriteLine("\t\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
                }
            }

            foreach (ISensor sensor in hardware.Sensors)
            {
                Console.WriteLine("\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
            }
        }

        foreach (var hardware in Computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("Core"))
                    {
                        return sensor.Value;
                    }
                }
            }
        }

        return null;
    }
}