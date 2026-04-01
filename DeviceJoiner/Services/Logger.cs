namespace DeviceJoiner.Services;

public class Logger
{
    private readonly string _logDir;
    private readonly string _logFile;
    private readonly object _lock = new();

    public event Action<string>? OnLog;

    public Logger(string logPath)
    {
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
            File.AppendAllText(_logFile, line + Environment.NewLine);
        }

        OnLog?.Invoke(line);
    }
}