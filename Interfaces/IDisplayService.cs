public interface IDisplayService
{
    void DisplayBalances(IEnumerable<CoinBalance> currentBalances, decimal usdToNokRate, decimal btcPrice);
}