public interface IDirectusService
{
    Task SendCoinValueAsync(string token, decimal balance, decimal price, decimal value, string source);
    Task SendTotalBalanceAsync(decimal usdtBalance);
} 