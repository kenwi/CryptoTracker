using Microsoft.Extensions.Options;
using System.Globalization;

public class BalanceDisplayService : IDisplayService
{
    private readonly IValueCalculationService _valueCalculationService;
    private readonly ExchangeRateConfig _exchangeRateConfig;
    private List<CoinBalance> _previousBalances = [];

    public BalanceDisplayService(
        IValueCalculationService valueCalculationService,
        IOptions<ExchangeRateConfig> exchangeRateConfig)
    {
        _valueCalculationService = valueCalculationService;
        _exchangeRateConfig = exchangeRateConfig.Value;
    }

    public void DisplayBalances(IEnumerable<CoinBalance> currentBalances, decimal usdExchangeRate, decimal btcPrice)
    {
        var usCulture = new CultureInfo("en-US");
        var culture = new CultureInfo(_exchangeRateConfig.Culture);
        var currencyName = _exchangeRateConfig.Currency;

        Console.WriteLine($"\n{DateTime.Now:G}");
        Console.WriteLine($"Coin     | Balance      | Price            | Value (USDT)      | Value ({currencyName})       | Change (USDT)  | Change % | Source");
        Console.WriteLine("--------------------------------------------------------------------------------------------------------------------------");

        foreach (var balance in currentBalances)
        {
            var previousBalance = _previousBalances.FirstOrDefault(p => 
                p.Asset == balance.Asset && 
                p.Source == balance.Source);
            
            bool isIncrease = previousBalance == null || balance.Value >= previousBalance.Value;
            
            Console.ForegroundColor = isIncrease ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
            if (previousBalance == null)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            var currencyValue = _valueCalculationService.CalculateCurrencyValue(balance.Value, usdExchangeRate);
            var valueChange = previousBalance != null ? balance.Value - previousBalance.Value : 0;
            var percentChange = previousBalance?.Value > 0 ? (valueChange / previousBalance.Value) * 100 : 0;

            var changeDisplay = previousBalance != null
                ? $"{(valueChange >= 0 ? "+" : "")}{valueChange.ToString("N2", usCulture)}"
                : "";
            var percentDisplay = previousBalance != null
                ? $"{(percentChange >= 0 ? "+" : "")}{percentChange:F2}%"
                : "";

            Console.WriteLine(
                "{0,-8} | {1,-12:F3} | {2,-16} | {3,-17} | {4,-17} | {5,-14} | {6,-8} | {7}",
                balance.Asset,
                balance.Balance,
                $"{balance.Price.ToString("N3", usCulture)} USDT",
                $"{balance.Value.ToString("N2", usCulture)} USDT",
                $"{currencyValue.ToString("N2", culture)} {_exchangeRateConfig.Currency}",
                changeDisplay,
                percentDisplay,
                balance.Source);
        }

        DisplayTotals(currentBalances, usdExchangeRate, btcPrice);
        _previousBalances = currentBalances.ToList();
    }

    private void DisplayTotals(IEnumerable<CoinBalance> currentBalances, decimal exchangeRate, decimal btcPrice)
    {
        var usCulture = new CultureInfo("en-US");
        var culture = new CultureInfo(_exchangeRateConfig.Culture);

        var totalValue = currentBalances.Sum(b => b.Value);
        var totalCurrencyValue = _valueCalculationService.CalculateCurrencyValue(totalValue, exchangeRate);
        var totalBtcValue = _valueCalculationService.CalculateBtcValue(totalValue, btcPrice);

        var previousTotal = _previousBalances.Sum(b => b.Value);
        var previousCurrencyTotal = _valueCalculationService.CalculateCurrencyValue(previousTotal, exchangeRate);
        var previousBtcTotal = _valueCalculationService.CalculateBtcValue(previousTotal, btcPrice);

        var usdChange = totalValue - previousTotal;
        var currencyChange = totalCurrencyValue - previousCurrencyTotal;
        var btcChange = totalBtcValue - previousBtcTotal;

        var usdChangePercent = previousTotal > 0 ? (usdChange / previousTotal) * 100 : 0;
        var currencyChangePercent = previousCurrencyTotal > 0 ? (currencyChange / previousCurrencyTotal) * 100 : 0;
        var btcChangePercent = previousBtcTotal > 0 ? (btcChange / previousBtcTotal) * 100 : 0;
        
        Console.ForegroundColor = usdChange >= 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
        Console.WriteLine($"\n{FormatTotal("USDT", totalValue, usdChange, usdChangePercent, usCulture)}");
        Console.WriteLine($"{FormatTotal(_exchangeRateConfig.Currency, totalCurrencyValue, currencyChange, currencyChangePercent, culture)}");
        Console.WriteLine($"{FormatTotal("BTC", totalBtcValue, btcChange, btcChangePercent, null, 8)}");
        Console.ResetColor();
    }

    private static string FormatTotal(string currency, decimal total, decimal change, decimal changePercent, IFormatProvider? culture = null, int decimals = 2)
    {
        var format = $"F{decimals}";
        var totalStr = culture != null 
            ? total.ToString("N" + decimals, culture) 
            : total.ToString(format);
        var changeStr = culture != null 
            ? change.ToString("N" + decimals, culture) 
            : change.ToString(format);

        var sign = change >= 0 ? "+" : "";
        var percentSign = changePercent >= 0 ? "+" : "";

        currency += ":";
        return $"Total {currency,-5} {totalStr} ({sign}{changeStr} / {percentSign}{changePercent:F2}%)";
    }
} 