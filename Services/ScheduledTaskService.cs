using System.Diagnostics;

namespace DeviceJoiner.Services;

public class ScheduledTaskService
{
    private const string TaskName = "DeviceJoinerAutoJoin";
    private const string RetryFile = ".djretry";
    private readonly string _retryFilePath;
    private readonly string _exePath;

    public ScheduledTaskService()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath)
            ?? AppDomain.CurrentDomain.BaseDirectory;
        _retryFilePath = Path.Combine(exeDir, RetryFile);
        _exePath = Environment.ProcessPath ?? "";
    }

    public void CreateTask()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/Create /SC ONSTART /TN \"{TaskName}\" /TR \"\\\"{_exePath}\\\" --autojoin\" /F /RL HIGHEST",
            UseShellExecute = true,
            Verb = "runas"
        };
        using var process = Process.Start(psi);
        process?.WaitForExit(30000);
    }

    public void DeleteTask()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/Delete /TN \"{TaskName}\" /F",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        process?.WaitForExit(30000);
    }

    public int GetRetryCount()
    {
        if (!File.Exists(_retryFilePath))
            return 0;
        var text = File.ReadAllText(_retryFilePath);
        return int.TryParse(text, out var count) ? count : 0;
    }

    public void IncrementRetryCount()
    {
        var count = GetRetryCount() + 1;
        File.WriteAllText(_retryFilePath, count.ToString());
    }

    public void ClearRetryCount()
    {
        if (File.Exists(_retryFilePath))
            File.Delete(_retryFilePath);
    }
}
