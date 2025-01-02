public interface IValueCalculationService
{
    decimal CalculateBtcValue(decimal usdValue, decimal btcPrice);
    decimal CalculateNokValue(decimal usdValue, decimal usdToNokRate);
}
