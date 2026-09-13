namespace DeviceJoiner.Config;

public class AppConfig
{
    public string HostnameTemplate { get; set; } = "{prefix}-{sn}";
    public string Prefix { get; set; } = "WS";
    public string Domain { get; set; } = "";
    public string DomainUser { get; set; } = "";
    public int MaxRetryCount { get; set; } = 3;
    public string LogPath { get; set; } = "logs/app.log";

    public void Normalize()
    {
        HostnameTemplate = string.IsNullOrWhiteSpace(HostnameTemplate) ? "{prefix}-{sn}" : HostnameTemplate;
        Prefix = Prefix?.Trim() ?? "";
        Domain = Domain?.Trim() ?? "";
        DomainUser = DomainUser?.Trim() ?? "";
        MaxRetryCount = Math.Clamp(MaxRetryCount, 1, 10);
        LogPath = string.IsNullOrWhiteSpace(LogPath) ? "logs/app.log" : LogPath.Trim();
    }
}
