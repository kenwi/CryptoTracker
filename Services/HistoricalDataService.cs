using Microsoft.Extensions.Logging;
using System.Globalization;

public class HistoricalDataService : IHistoricalDataService
{
    private readonly ILogger<HistoricalDataService> _logger;

    public HistoricalDataService(ILogger<HistoricalDataService> logger)
    {
        _logger = logger;
    }

    public async Task ViewHistoricalDataAsync(HistoricalDataViewOptions options)
    {
        try
        {
            if (!File.Exists(options.FilePath))
            {
                _logger.LogError("File not found: {FilePath}", options.FilePath);
                return;
            }

            var lines = await File.ReadAllLinesAsync(options.FilePath);
            if (lines.Length < 2) // Header + at least one data row
            {
                _logger.LogError("File contains no data");
                return;
            }

            var data = ParseCsvData(lines);
            var filteredData = FilterData(data, options);
            DisplayHistoricalData(filteredData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing historical data");
        }
    }

    public async Task ListUniqueAssetsAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length < 2)
            {
                _logger.LogError("File contains no data");
                return;
            }

            var data = ParseCsvData(lines);
            var uniqueAssets = data
                .GroupBy(d => new { d.Asset, d.Source })
                .Select(g => g.First())
                .OrderBy(d => d.Asset)
                .ThenBy(d => d.Source);

            Console.WriteLine("\nAvailable Assets:");
            Console.WriteLine("Asset | Source");
            Console.WriteLine("----------------");

            foreach (var entry in uniqueAssets)
            {
                Console.WriteLine($"{entry.Asset,-5} | {entry.Source}");
            }

            Console.WriteLine();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing unique assets");
        }
    }

    private List<HistoricalDataEntry> ParseCsvData(string[] lines)
    {
        var data = new List<HistoricalDataEntry>();
        var header = lines[0].Split(',');
        var culture = new CultureInfo("en-US")
        {
            NumberFormat = 
            { 
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = ""
            }
        };

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                var values = lines[i].Split(',');
                if (values.Length != header.Length) continue;

                data.Add(new HistoricalDataEntry
                {
                    Timestamp = DateTime.Parse(values[0]),
                    Asset = values[1],
                    Balance = decimal.Parse(values[2], NumberStyles.Any, culture),
                    Price = decimal.Parse(values[3], NumberStyles.Any, culture),
                    Value = decimal.Parse(values[4], NumberStyles.Any, culture),
                    NokValue = decimal.Parse(values[5], NumberStyles.Any, culture),
                    BtcValue = decimal.Parse(values[6], NumberStyles.Any, culture),
                    Source = values[7]
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse line {LineNumber}: {Line}", i + 1, lines[i]);
            }
        }

        return data;
    }

    private IEnumerable<HistoricalDataEntry> FilterData(
        List<HistoricalDataEntry> data, 
        HistoricalDataViewOptions options)
    {
        var query = data.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(options.AssetFilter))
        {
            query = query.Where(d => d.Asset.Equals(options.AssetFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(options.SourceFilter))
        {
            query = query.Where(d => d.Source.Equals(options.SourceFilter, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderBy(d => d.Timestamp);
    }

    private void DisplayHistoricalData(IEnumerable<HistoricalDataEntry> data)
    {
        var usCulture = new CultureInfo("en-US");
        var nokCulture = new CultureInfo("nb-NO");

        Console.WriteLine("\nHistorical Data:");
        Console.WriteLine("Timestamp           | Asset | Balance      | Price (USDT)  | Value (USDT)  | Value (NOK)   | Value (BTC)   | Source");
        Console.WriteLine("-------------------------------------------------------------------------------------------------------------------");

        foreach (var entry in data)
        {
            Console.WriteLine(
                $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} | " +
                $"{entry.Asset,-5} | " +
                $"{entry.Balance.ToString("F3", usCulture),-12} | " +
                $"{entry.Price.ToString("F3", usCulture),-13} | " +
                $"{entry.Value.ToString("F2", usCulture),-13} | " +
                $"{entry.NokValue.ToString("F2", nokCulture),-13} | " +
                $"{entry.BtcValue.ToString("F8", usCulture),-13} | " +
                $"{entry.Source}");
        }
    }
}
