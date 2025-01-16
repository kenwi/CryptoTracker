public interface IExchangeRateService
{
    Task<decimal> GetUsdExchangeRate();
} 