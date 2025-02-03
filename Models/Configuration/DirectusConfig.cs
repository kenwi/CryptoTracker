public class DirectusConfig
{
    public string Host { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string CoinValuesEndpoint { get; set; } = string.Empty;
    public string TotalBalanceEndpoint { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
    public bool LoggingEnabled { get; set; } = false;
} 