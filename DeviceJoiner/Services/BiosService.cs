using System.Management;

namespace DeviceJoiner.Services;

public class BiosService
{
    public string GetSerialNumber()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT SerialNumber FROM Win32_BIOS");
        using var collection = searcher.Get();

        foreach (ManagementBaseObject obj in collection)
        {
            var sn = obj["SerialNumber"]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(sn))
                return sn;
        }

        throw new InvalidOperationException("无法从 BIOS 读取序列号");
    }

    public string GetCurrentHostname()
    {
        return Environment.MachineName;
    }
}
