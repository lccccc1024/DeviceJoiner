using System.Text.Json;

namespace DeviceJoiner.Config;

public class ConfigManager
{
    private readonly string _configPath;

    public ConfigManager(string? configPath = null)
    {
        _configPath = configPath ?? Path.Combine(
            ExeDirectory, "config", "appsettings.json");
    }

    private static string ExeDirectory =>
        Path.GetDirectoryName(Environment.ProcessPath)
        ?? AppDomain.CurrentDomain.BaseDirectory;

    public AppConfig Load()
    {
        if (!File.Exists(_configPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            config.Normalize();
            return config;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceError($"配置文件格式无效: {ex.Message}");
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(config, options);
        var tempPath = _configPath + ".tmp";
        File.WriteAllText(tempPath, json, System.Text.Encoding.UTF8);
        File.Move(tempPath, _configPath, true);
    }
}
