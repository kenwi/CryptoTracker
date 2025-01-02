public interface IDirectusService
{
    Task SendCoinValueAsync(string token, decimal balance, decimal price, decimal value, string source, decimal btcValue);
    Task SendTotalBalanceAsync(decimal usdtBalance, decimal totalBtcValue);
} 