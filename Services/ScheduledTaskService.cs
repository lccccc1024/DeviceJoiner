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
        var taskCommand = $"\"{_exePath}\" --autojoin";
        RunSchtasks(new[] { "/Create", "/SC", "ONSTART", "/TN", TaskName,
            "/TR", taskCommand, "/RU", "SYSTEM", "/RL", "HIGHEST", "/F" }, 0);
    }

    public void DeleteTask()
    {
        RunSchtasks(new[] { "/Delete", "/TN", TaskName, "/F" }, 0, 1);
    }

    private static void RunSchtasks(IEnumerable<string> arguments, params int[] acceptableExitCodes)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "schtasks",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);
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
        return int.TryParse(text, out var count) && count >= 0 ? count : 0;
    }

    public void IncrementRetryCount()
    {
        var count = GetRetryCount() + 1;
        var tempPath = _retryFilePath + ".tmp";
        File.WriteAllText(tempPath, count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        File.Move(tempPath, _retryFilePath, true);
    }

    public void ClearRetryCount()
    {
        if (File.Exists(_retryFilePath))
            File.Delete(_retryFilePath);
    }
}
