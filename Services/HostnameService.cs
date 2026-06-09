using System.Management;
using System.Text.RegularExpressions;

namespace DeviceJoiner.Services;

public class HostnameService
{
    public string BuildHostname(string template, string prefix, string sn)
    {
        return template
            .Replace("{prefix}", prefix)
            .Replace("{sn}", sn);
    }

    public bool IsValidHostname(string hostname)
    {
        if (string.IsNullOrEmpty(hostname)) return false;
        if (hostname.Length > 15) return false;
        return Regex.IsMatch(hostname, @"^[a-zA-Z0-9]([a-zA-Z0-9\-]*[a-zA-Z0-9])?$");
    }

    public void SetHostname(string newHostname)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_ComputerSystem");
        using var collection = searcher.Get();

        foreach (ManagementBaseObject obj in collection)
        {
            using var computer = (ManagementObject)obj;
            var returnValue = Convert.ToInt32(computer.InvokeMethod("Rename", new object[] { newHostname, "", "" }));
            if (returnValue != 0)
                throw new InvalidOperationException($"修改主机名失败，返回码: {returnValue}");
            break;
        }
    }
}
