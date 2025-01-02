public interface IExportService
{
    Task ExportBalancesAsync(
        IEnumerable<CoinBalance> balances, 
        decimal usdToNokRate, 
        decimal btcPrice);
}
