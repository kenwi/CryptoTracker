public record CoinBalance
{
    public string Asset { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public decimal Price { get; init; }
    public decimal Value { get; init; }
    public string Source { get; init; } = string.Empty;
} 