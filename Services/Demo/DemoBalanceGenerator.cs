using Binance.Net.Objects.Models.Spot;

public class DemoBalanceGenerator
{
    private static readonly string[] DemoCoins = [
        "BTC",
        "ETH",
        "BNB",
        "SOL",
        "AVAX",
        "DOGE",
        "XRP",
        "LINK",
        "ADA",
        "XLM",
        "TRX",
        "IO",
        "ETC",
        "SHIB"
        ];

    private readonly Random _random = new();

    public IReadOnlyList<BinanceBalance> GenerateManualBalances()
    {
        return DemoCoins.Select(coin => new BinanceBalance
        {
            Asset = coin,
            Available = (decimal)Math.Round(_random.NextDouble() * (coin == "BTC" ? 2 : 100), 4)
        }).ToList();
    }
} 