public interface IValueCalculationService
{
    decimal CalculateBtcValue(decimal usdValue, decimal btcPrice);
    decimal CalculateCurrencyValue(decimal usdValue, decimal usdExchangeRate);
}
