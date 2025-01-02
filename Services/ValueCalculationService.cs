public class ValueCalculationService : IValueCalculationService
{
    public decimal CalculateBtcValue(decimal usdValue, decimal btcPrice)
    {
        return btcPrice > 0 ? usdValue / btcPrice : 0;
    }

    public decimal CalculateNokValue(decimal usdValue, decimal usdToNokRate)
    {
        return usdValue * usdToNokRate;
    }
} 