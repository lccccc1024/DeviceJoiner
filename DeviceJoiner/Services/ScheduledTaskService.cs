namespace DeviceJoiner.Services;

public class ScheduledTaskService
{
    private const string TaskName = "DeviceJoinerAutoJoin";
    private const string CounterFileName = ".djretry";

    public int GetRetryCount()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(exeDir, CounterFileName);
        
        if (!File.Exists(filePath))
            return 0;
        
        var content = File.ReadAllText(filePath).Trim();
        return int.TryParse(content, out var count) ? count : 0;
    }

    public void IncrementRetryCount()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(exeDir, CounterFileName);
        var count = GetRetryCount() + 1;
        File.WriteAllText(filePath, count.ToString());
    }

    public void ClearRetryCount()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(exeDir, CounterFileName);
        
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public void DeleteTask()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/delete /tn \"{TaskName}\" /f",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(psi)?.WaitForExit();
        }
        catch
        {
        }
    }

    public void CreateTask()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? "";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/create /sc onstart /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --autojoin\" /rl limited /f",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            System.Diagnostics.Process.Start(psi)?.WaitForExit();
        }
        catch
        {
        }
    }
}