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
        _exePath = Environment.ProcessPath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName);
    }

    public void CreateTask()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = $"/Create /SC ONSTART /TN \"{TaskName}\" /TR \"\\\"{_exePath}\\\" --autojoin\" /F /RL HIGHEST",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("无法启动 schtasks 创建计划任务");
        process.WaitForExit(30000);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"创建计划任务失败 (schtasks 退出码: {process.ExitCode})");
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
        if (process == null)
            throw new InvalidOperationException("无法启动 schtasks 删除计划任务");
        process.WaitForExit(30000);
        // schtasks /Delete returns 0 on success, 1 if task doesn't exist — both are acceptable
        if (process.ExitCode != 0 && process.ExitCode != 1)
            throw new InvalidOperationException($"删除计划任务失败 (schtasks 退出码: {process.ExitCode})");
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
