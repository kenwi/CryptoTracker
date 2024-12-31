using Binance.Net.Objects.Models.Spot;

public interface IBinanceService
{
    Task<IEnumerable<CoinBalance>> GetDetailedBalancesAsync(IEnumerable<BinanceBalance> manualBalances);

    Task<BinancePrice> GetPriceAsync(string symbol);
}
