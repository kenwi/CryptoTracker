public class BinanceConfig
{
    public string Secret { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public List<string> ExcludedSymbols { get; set; } = [];
} 