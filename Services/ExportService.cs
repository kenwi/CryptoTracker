using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly ExportConfig _config;
    private readonly IValueCalculationService _valueCalculationService;

    public ExportService(
        ILogger<ExportService> logger,
        IOptions<ExportConfig> config,
        IValueCalculationService valueCalculationService)
    {
        _logger = logger;
        _config = config.Value;
        _valueCalculationService = valueCalculationService;
    }

    public async Task ExportBalancesAsync(
        IEnumerable<CoinBalance> balances, 
        decimal usdToNokRate, 
        decimal btcPrice)
    {
        if (!_config.Enabled) return;

        try
        {
            Directory.CreateDirectory(_config.OutputPath);
            var valuesPath = Path.Combine(_config.OutputPath, $"{_config.ValuesFilename}.{_config.Format}");
            var totalsPath = Path.Combine(_config.OutputPath, $"{_config.TotalsFilename}.{_config.Format}");

            await (_config.Format.ToLower() switch
            {
                "csv" => ExportToCsvAsync(balances, usdToNokRate, btcPrice, valuesPath, totalsPath),
                //"excel" => ExportToExcelAsync(balances, usdToNokRate, btcPrice, valuesPath, totalsPath),
                "json" => ExportToJsonAsync(balances, usdToNokRate, btcPrice, valuesPath, totalsPath),
                _ => throw new ArgumentException($"Unsupported format: {_config.Format}")
            });

            _logger.LogInformation("Exported portfolio to {ValuesPath} and {TotalsPath}", valuesPath, totalsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export portfolio");
        }
    }

    private async Task ExportToCsvAsync(
        IEnumerable<CoinBalance> balances, 
        decimal usdToNokRate, 
        decimal btcPrice, 
        string valuesPath,
        string totalsPath)
    {
        var timestamp = DateTime.Now;
        
        // Export individual values
        var valueLines = new List<string>();
        if (!File.Exists(valuesPath))
        {
            valueLines.Add("Timestamp,Asset,Balance,Price (USDT),Value (USDT),Value (NOK),Value (BTC),Source");
        }

        foreach (var balance in balances)
        {
            var nokValue = _valueCalculationService.CalculateNokValue(balance.Value, usdToNokRate);
            var btcValue = _valueCalculationService.CalculateBtcValue(balance.Value, btcPrice);

            valueLines.Add($"{timestamp:yyyy-MM-dd HH:mm:ss},{balance.Asset},{balance.Balance}," +
                         $"{balance.Price},{balance.Value},{nokValue},{btcValue},{balance.Source}");
        }

        await File.AppendAllLinesAsync(valuesPath, valueLines);

        // Export totals
        var totalLines = new List<string>();
        if (!File.Exists(totalsPath))
        {
            totalLines.Add("Timestamp,Total (USDT),Total (NOK),Total (BTC)");
        }

        var totalValue = balances.Sum(b => b.Value);
        var totalNokValue = _valueCalculationService.CalculateNokValue(totalValue, usdToNokRate);
        var totalBtcValue = _valueCalculationService.CalculateBtcValue(totalValue, btcPrice);

        totalLines.Add($"{timestamp:yyyy-MM-dd HH:mm:ss},{totalValue},{totalNokValue},{totalBtcValue}");
        
        await File.AppendAllLinesAsync(totalsPath, totalLines);
    }

    private async Task ExportToJsonAsync(
        IEnumerable<CoinBalance> balances, 
        decimal usdToNokRate, 
        decimal btcPrice, 
        string valuesPath,
        string totalsPath)
    {
        var timestamp = DateTime.Now;
        var values = new
        {
            Timestamp = timestamp,
            Balances = balances.Select(b => new
            {
                b.Asset,
                b.Balance,
                Price = b.Price,
                UsdValue = b.Value,
                NokValue = _valueCalculationService.CalculateNokValue(b.Value, usdToNokRate),
                BtcValue = _valueCalculationService.CalculateBtcValue(b.Value, btcPrice),
                b.Source
            })
        };

        var totals = new
        {
            Timestamp = timestamp,
            UsdValue = balances.Sum(b => b.Value),
            NokValue = balances.Sum(b => _valueCalculationService.CalculateNokValue(b.Value, usdToNokRate)),
            BtcValue = balances.Sum(b => _valueCalculationService.CalculateBtcValue(b.Value, btcPrice))
        };

        // Append to existing arrays or create new ones
        var valuesArray = await ReadJsonArrayAsync(valuesPath);
        valuesArray.Add(values);
        await WriteJsonArrayAsync(valuesPath, valuesArray);

        var totalsArray = await ReadJsonArrayAsync(totalsPath);
        totalsArray.Add(totals);
        await WriteJsonArrayAsync(totalsPath, totalsArray);
    }

    private async Task<JsonArray> ReadJsonArrayAsync(string path)
    {
        if (!File.Exists(path))
            return new JsonArray();

        var content = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<JsonArray>(content) ?? new JsonArray();
    }

    private async Task WriteJsonArrayAsync(string path, JsonArray array)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(array, options));
    }

    //private async Task ExportToExcelAsync(
    //    IEnumerable<CoinBalance> balances, 
    //    decimal usdToNokRate, 
    //    decimal btcPrice, 
    //    string valuesPath,
    //    string totalsPath)
    //{
    //    // Requires NuGet package: EPPlus
    //    throw new NotImplementedException("Excel export not yet implemented");
    //}
} 