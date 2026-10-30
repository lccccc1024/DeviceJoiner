namespace DeviceJoiner.Services;

public class Logger
{
    private readonly string _logDir;
    private readonly string _logFile;
    private readonly long _maxLogBytes;
    private readonly object _lock = new();

    private const long DefaultMaxLogBytes = 5 * 1024 * 1024;

    public event Action<string>? OnLog;

    public Logger(string logPath, long maxLogBytes = DefaultMaxLogBytes)
    {
        _maxLogBytes = maxLogBytes > 0 ? maxLogBytes : DefaultMaxLogBytes;
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath)
            ?? AppDomain.CurrentDomain.BaseDirectory;

        _logDir = Path.IsPathRooted(logPath)
            ? Path.GetDirectoryName(logPath) ?? "."
            : Path.Combine(exeDir, Path.GetDirectoryName(logPath) ?? "logs");
        _logFile = Path.IsPathRooted(logPath)
            ? logPath
            : Path.Combine(exeDir, logPath);

        if (!Directory.Exists(_logDir))
            Directory.CreateDirectory(_logDir);
    }

    public void Info(string message) => WriteLog("INFO", message);
    public void Warn(string message) => WriteLog("WARN", message);
    public void Error(string message) => WriteLog("ERROR", message);

    private void WriteLog(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var line = $"{timestamp} [{level}] {message}";

        lock (_lock)
        {
            RotateIfNeeded();
            File.AppendAllText(_logFile, line + Environment.NewLine);
        }

        OnLog?.Invoke(line);
    }

    /// <summary>超过 5 MB 就滚成 .1（只保留一份备份），避免批量部署时日志无限增长。</summary>
    private void RotateIfNeeded()
    {
        try
        {
            var info = new FileInfo(_logFile);
            if (!info.Exists || info.Length < _maxLogBytes) return;

            var backup = _logFile + ".1";
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(_logFile, backup);
        }
        catch
        {
            // 轮转失败不应阻止写日志
        }
    }
}