namespace DeviceJoiner.Config;

public class AppConfig
{
    public string HostnameTemplate { get; set; } = "{prefix}-{sn}";
    public string Prefix { get; set; } = "WS";
    public string Domain { get; set; } = "";
    public string DomainUser { get; set; } = "";
    public string DomainPassword { get; set; } = "";
    public int MaxRetryCount { get; set; } = 3;
    public string LogPath { get; set; } = "logs/app.log";
}
