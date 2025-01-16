public interface IDisplayService
{
    void DisplayBalances(IEnumerable<CoinBalance> currentBalances, decimal usdExchangeRate, decimal btcPrice);
}