public interface IExportService
{
    Task ExportBalancesAsync(
        IEnumerable<CoinBalance> balances, 
        decimal usdExchangeRate, 
        decimal btcPrice);
}
