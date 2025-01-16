public class CoinBalance
{
    public DateTime Timestamp { get; set; }
    public string Asset { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal Price { get; set; }
    public decimal Value { get; set; }
    public string Source { get; set; } = string.Empty;
} 