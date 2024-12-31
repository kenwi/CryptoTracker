public interface IExchangeRateService
{
    Task<decimal> GetUsdToNokRateAsync();
} 