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
        var arguments = $"/Create /SC ONSTART /TN \"{TaskName}\" /TR \"\\\"{_exePath}\\\" --autojoin\" /F /RL HIGHEST";
        RunSchtasks(arguments, 0);
    }

    public void DeleteTask()
    {
        var arguments = $"/Delete /TN \"{TaskName}\" /F";
        RunSchtasks(arguments, 0, 1);
    }

    private static void RunSchtasks(string arguments, params int[] acceptableExitCodes)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("无法启动 schtasks");

        if (!process.WaitForExit(30000))
        {
            try { process.Kill(); } catch { /* best effort */ }
            throw new InvalidOperationException("schtasks 执行超时 (30秒)");
        }

        if (acceptableExitCodes.Length > 0 && !acceptableExitCodes.Contains(process.ExitCode))
            throw new InvalidOperationException($"schtasks 失败 (退出码: {process.ExitCode})");
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
