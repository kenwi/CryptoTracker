using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly ExportConfig _exportConfig;
    private readonly ExchangeRateConfig _exchangeRateConfig;
    private readonly IValueCalculationService _valueCalculationService;

    public ExportService(
        ILogger<ExportService> logger,
        IOptions<ExportConfig> exportConfig,
        IOptions<ExchangeRateConfig> exchangeRateConfig,
        IValueCalculationService valueCalculationService)
    {
        _logger = logger;
        _exportConfig = exportConfig.Value;
        _exchangeRateConfig = exchangeRateConfig.Value;
        _valueCalculationService = valueCalculationService;
    }

    public async Task ExportBalancesAsync(
        IEnumerable<CoinBalance> balances, 
        decimal usdExchangeRate, 
        decimal btcPrice)
    {
        if (!_exportConfig.Enabled) return;

        try
        {
            Directory.CreateDirectory(_exportConfig.OutputPath);
            var valuesPath = Path.Combine(_exportConfig.OutputPath, $"{_exportConfig.ValuesFilename}.{_exportConfig.Format}");
            var totalsPath = Path.Combine(_exportConfig.OutputPath, $"{_exportConfig.TotalsFilename}.{_exportConfig.Format}");

            await (_exportConfig.Format.ToLower() switch
            {
                "csv" => ExportToCsvAsync(balances, usdExchangeRate, btcPrice, valuesPath, totalsPath),
                "xlsx" => ExportToExcelAsync(balances, usdExchangeRate, btcPrice, valuesPath, totalsPath),
                "json" => ExportToJsonAsync(balances, usdExchangeRate, btcPrice, valuesPath, totalsPath),
                _ => throw new ArgumentException($"Unsupported format: {_exportConfig.Format}")
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
        decimal usdExchangeRate, 
        decimal btcPrice, 
        string valuesPath,
        string totalsPath)
    {
        var timestamp = DateTime.Now;
        var usCulture = new CultureInfo("en-US");
        usCulture.NumberFormat.NumberGroupSeparator = "";  // Remove thousand separators
        
        // Export individual values
        var valueLines = new List<string>();
        if (!File.Exists(valuesPath))
        {
            valueLines.Add($"Timestamp,Asset,Balance,Price (USDT),Value (USDT),Value ({_exchangeRateConfig.Currency}),Value (BTC),Source");
        }

        foreach (var balance in balances)
        {
            var currencyValue = _valueCalculationService.CalculateCurrencyValue(balance.Value, usdExchangeRate);
            var btcValue = _valueCalculationService.CalculateBtcValue(balance.Value, btcPrice);

            valueLines.Add($"{balance.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"{balance.Asset}," +
                          $"{balance.Balance.ToString("F3", usCulture)}," +
                          $"{balance.Price.ToString("F3", usCulture)}," +
                          $"{balance.Value.ToString("F2", usCulture)}," +
                          $"{currencyValue.ToString("F2", usCulture)}," +
                          $"{btcValue.ToString("F8", usCulture)}," +
                          $"{balance.Source}");
        }

        await File.AppendAllLinesAsync(valuesPath, valueLines);

        // Export totals
        var totalLines = new List<string>();
        if (!File.Exists(totalsPath))
        {
            totalLines.Add($"Timestamp,Total (USDT),Total ({_exchangeRateConfig.Currency}),Total (BTC)");
        }

        var totalValue = balances.Sum(b => b.Value);
        var totalCurrencyValue = _valueCalculationService.CalculateCurrencyValue(totalValue, usdExchangeRate);
        var totalBtcValue = _valueCalculationService.CalculateBtcValue(totalValue, btcPrice);

        totalLines.Add($"{timestamp:yyyy-MM-dd HH:mm:ss}," +
                      $"{totalValue.ToString("F2", usCulture)}," +
                      $"{totalCurrencyValue.ToString("F2", usCulture)}," +
                      $"{totalBtcValue.ToString("F8", usCulture)}");
        
        await File.AppendAllLinesAsync(totalsPath, totalLines);
    }

    private async Task ExportToJsonAsync(
        IEnumerable<CoinBalance> balances, 
        decimal usdExchangeRate, 
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
                CurrencyValue = _valueCalculationService.CalculateCurrencyValue(b.Value, usdExchangeRate),
                BtcValue = _valueCalculationService.CalculateBtcValue(b.Value, btcPrice),
                b.Source
            })
        };

        var totals = new
        {
            Timestamp = timestamp,
            UsdValue = balances.Sum(b => b.Value),
            CurrencyValue = balances.Sum(b => _valueCalculationService.CalculateCurrencyValue(b.Value, usdExchangeRate)),
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

    private async Task ExportToExcelAsync(
        IEnumerable<CoinBalance> balances, 
        decimal usdExchangeRate, 
        decimal btcPrice, 
        string valuesPath,
        string totalsPath)
    {
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        await ExportValuesToExcelAsync(balances, usdExchangeRate, btcPrice, valuesPath);
        await ExportTotalsToExcelAsync(balances, usdExchangeRate, btcPrice, totalsPath);
    }

    private async Task ExportValuesToExcelAsync(
        IEnumerable<CoinBalance> balances,
        decimal usdExchangeRate,
        decimal btcPrice,
        string path)
    {
        //var timestamp = DateTime.Now;
        var file = new FileInfo(path);
        
        try
        {
            // Create a temporary file path
            var tempPath = Path.GetTempFileName();
            _logger.LogDebug("Using temporary file: {TempPath}", tempPath);

            // If the file exists, copy it to our temp location first
            if (file.Exists)
            {
                File.Copy(file.FullName, tempPath, true);
            }

            using (var package = new ExcelPackage(new FileInfo(tempPath)))
            {
                _logger.LogDebug("Created Excel package");

                var sheet = package.Workbook.Worksheets.FirstOrDefault() ?? 
                           package.Workbook.Worksheets.Add("Portfolio Values");
                _logger.LogDebug("Using worksheet: {SheetName}", sheet.Name);

                // Add headers if sheet is new
                if (sheet.Dimension == null)
                {
                    _logger.LogDebug("Adding headers to new sheet");
                    string[] headers = { "Timestamp", "Asset", "Balance", "Price (USDT)", 
                                       "Value (USDT)", $"Value ({_exchangeRateConfig.Currency}", "Value (BTC)", "Source" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        sheet.Cells[1, i + 1].Value = headers[i];
                    }

                    // Style headers
                    var headerRange = sheet.Cells[1, 1, 1, headers.Length];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                // Get next row
                int row = (sheet.Dimension?.Rows ?? 1) + 1;
                _logger.LogDebug("Starting data entry at row: {Row}", row);

                // Add new data
                foreach (var balance in balances)
                {
                    var currencyValue = _valueCalculationService.CalculateCurrencyValue(balance.Value, usdExchangeRate);
                    var btcValue = _valueCalculationService.CalculateBtcValue(balance.Value, btcPrice);

                    sheet.Cells[row, 1].Value = balance.Timestamp;
                    sheet.Cells[row, 2].Value = balance.Asset;
                    sheet.Cells[row, 3].Value = balance.Balance;
                    sheet.Cells[row, 4].Value = balance.Price;
                    sheet.Cells[row, 5].Value = balance.Value;
                    sheet.Cells[row, 6].Value = currencyValue;
                    sheet.Cells[row, 7].Value = btcValue;
                    sheet.Cells[row, 8].Value = balance.Source;

                    row++;
                }

                // Format columns
                sheet.Column(1).Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                sheet.Column(3).Style.Numberformat.Format = "#,##0.000";
                sheet.Column(4).Style.Numberformat.Format = "#,##0.000";
                sheet.Column(5).Style.Numberformat.Format = "#,##0.00";
                sheet.Column(6).Style.Numberformat.Format = "#,##0.00";
                sheet.Column(7).Style.Numberformat.Format = "0.00000000";

                // Convert range to table if it isn't already
                if (!sheet.Tables.Any())
                {
                    var dimension = sheet.Dimension;
                    if (dimension != null)
                    {
                        var dataRange = sheet.Cells[1, 1, dimension.End.Row, dimension.End.Column];
                        var table = sheet.Tables.Add(dataRange, "PortfolioValues");
                        table.ShowHeader = true;
                        table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;
                    }
                }
                else
                {
                    // Update existing table range
                    var table = sheet.Tables[0];
                    var dimension = sheet.Dimension;
                    if (dimension != null && table != null)
                    {
                        table.AddRow(dimension.End.Row - table.Address.End.Row);
                    }
                }

                // Add or update chart if it doesn't exist
                if (!sheet.Drawings.Any(d => d.Name == "AssetValuesChart"))
                {
                    _logger.LogDebug("Updating existing asset values chart");
                    
                    // Remove existing chart
                    var existingChart = sheet.Drawings.FirstOrDefault(d => d.Name == "AssetValuesChart");
                    if (existingChart != null)
                    {
                        sheet.Drawings.Remove(existingChart);
                    }

                    // Calculate chart position (to the right of the data)
                    var lastColumn = sheet.Dimension?.End.Column ?? 8; // Default to 8 if null
                    var chartStartColumn = lastColumn + 1; // Start one column after the data
                    
                    var chart = sheet.Drawings.AddChart("AssetValuesChart", eChartType.Line);
                    
                    // Position the chart (row 1, next to last column)
                    chart.SetPosition(0, 0, chartStartColumn, 0);
                    chart.SetSize(800, 400);
                    
                    // Configure the chart
                    chart.Title.Text = $"Asset Values ({_exchangeRateConfig.Currency}) Over Time";
                    chart.XAxis.Title.Text = "Date";
                    chart.YAxis.Title.Text = $"{_exchangeRateConfig.Currency} Value";
                    
                    // Add the data series grouped by Asset
                    var dimension = sheet.Dimension;
                    if (dimension != null)
                    {
                        var assets = sheet.Cells[2, 2, dimension.End.Row, 2]
                            .Select(c => c.Value?.ToString())
                            .Where(v => !string.IsNullOrEmpty(v))
                            .Distinct();

                        foreach (var asset in assets)
                        {
                            var assetRows = Enumerable.Range(2, dimension.End.Row - 1)
                                .Where(r => sheet.Cells[r, 2].Value?.ToString() == asset)
                                .ToList();

                            if (!assetRows.Any()) continue;

                            var currencyValues = sheet.Cells[assetRows[0], 6, assetRows[^1], 6];
                            var timestamps = sheet.Cells[assetRows[0], 1, assetRows[^1], 1];

                            var series = chart.Series.Add(currencyValues, timestamps);
                            series.Header = asset;
                        }
                    }
                    
                    // Format axis
                    chart.XAxis.Format = "dd-MMM-yy HH:mm";
                    chart.XAxis.TickLabelPosition = eTickLabelPosition.Low;
                    chart.XAxis.Font.Size = 8;
                    //chart.XAxis.Orientation = eTextOrientation.UpRight;
                    chart.YAxis.Format = "#,##0";
                    chart.YAxis.MajorGridlines.Fill.Color = Color.LightGray;
                    
                    // Style the chart
                    chart.Style = eChartStyle.Style2;
                    chart.Legend.Position = eLegendPosition.Right;
                }

                sheet.Cells.AutoFitColumns();

                _logger.LogDebug("Saving Excel package to temp file");
                await package.SaveAsync();
            }

            // Ensure the target directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(file.FullName)!);

            // Copy the temp file to the final destination
            _logger.LogDebug("Copying from temp file to final destination");
            File.Copy(tempPath, file.FullName, true);
            
            // Clean up temp file
            File.Delete(tempPath);

            if (File.Exists(file.FullName))
            {
                _logger.LogInformation("Successfully created Excel file at: {Path}", file.FullName);
            }
            else
            {
                _logger.LogError("Failed to verify file creation at: {Path}", file.FullName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Excel file to {Path}", file.FullName);
            throw;
        }
    }

    private async Task ExportTotalsToExcelAsync(
        IEnumerable<CoinBalance> balances,
        decimal usdExchangeRate,
        decimal btcPrice,
        string path)
    {
        //var timestamp = DateTime.Now;
        var file = new FileInfo(path);
        
        try
        {
            // Create a temporary file path
            var tempPath = Path.GetTempFileName();
            _logger.LogDebug("Using temporary file for totals: {TempPath}", tempPath);

            // If the file exists, copy it to our temp location first
            if (file.Exists)
            {
                File.Copy(file.FullName, tempPath, true);
            }

            using (var package = new ExcelPackage(new FileInfo(tempPath)))
            {
                _logger.LogDebug("Created Excel package for totals");

                var sheet = package.Workbook.Worksheets.FirstOrDefault() ?? 
                           package.Workbook.Worksheets.Add("Portfolio Totals");
                _logger.LogDebug("Using worksheet: {SheetName}", sheet.Name);

                // Add headers if sheet is new
                if (sheet.Dimension == null)
                {
                    string[] headers = { "Timestamp", "Total (USDT)", $"Total ({_exchangeRateConfig.Currency})", "Total (BTC)" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        sheet.Cells[1, i + 1].Value = headers[i];
                    }

                    // Style headers
                    var headerRange = sheet.Cells[1, 1, 1, headers.Length];
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                // Get next row
                int row = (sheet.Dimension?.Rows ?? 1) + 1;
                _logger.LogDebug("Starting totals entry at row: {Row}", row);

                // Group the balances by time
                var interval = new TimeSpan(0, 1, 0);
                var groupedBalances = balances
                    .GroupBy(b => b.Timestamp.Ticks / interval.Ticks);

                // Calculate and add new totals
                foreach (var group in groupedBalances)
                {
                    var totalValue = group.Sum(b => b.Value);
                    var totalCurrencyValue = _valueCalculationService.CalculateCurrencyValue(totalValue, usdExchangeRate);
                    var totalBtcValue = _valueCalculationService.CalculateBtcValue(totalValue, btcPrice);

                    sheet.Cells[row, 1].Value = group.LastOrDefault()?.Timestamp;
                    sheet.Cells[row, 2].Value = totalValue;
                    sheet.Cells[row, 3].Value = totalCurrencyValue;
                    sheet.Cells[row, 4].Value = totalBtcValue;
                    row++;
                }

                // Format columns
                sheet.Column(1).Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                sheet.Column(2).Style.Numberformat.Format = "#,##0.00";
                sheet.Column(3).Style.Numberformat.Format = "#,##0.00";
                sheet.Column(4).Style.Numberformat.Format = "0.00000000";

                sheet.Cells.AutoFitColumns();

                // Add or update chart if it doesn't exist
                if (!sheet.Drawings.Any(d => d.Name == "PortfolioChart"))
                {
                    _logger.LogDebug("Adding portfolio value chart");
                    
                    // Calculate chart position (to the right of the data)
                    var lastColumn = sheet.Dimension?.End.Column ?? 4; // Default to 4 if null
                    var chartStartColumn = lastColumn + 1; // Start one column after the data
                    
                    var chart = sheet.Drawings.AddChart("PortfolioChart", eChartType.Line);
                    
                    // Position the chart (row 1, next to last column)
                    chart.SetPosition(0, 0, chartStartColumn, 0);
                    chart.SetSize(800, 400);
                    
                    // Configure the chart
                    chart.Title.Text = $"Portfolio Value ({_exchangeRateConfig.Currency}) Over Time";
                    chart.XAxis.Title.Text = "Date";
                    chart.YAxis.Title.Text = $"{_exchangeRateConfig.Currency} Value";
                    
                    // Add the data series
                    var lastRow = sheet.Dimension?.End.Row ?? 2; // Default to 2 if null
                    var currencyValues = sheet.Cells[2, 3, lastRow, 3]; // Currency values (column C)
                    var timestamps = sheet.Cells[2, 1, lastRow, 1]; // Timestamps (column A)
                    
                    var series = chart.Series.Add(currencyValues, timestamps);
                    series.Header = $"{_exchangeRateConfig.Currency} Value";
                    
                    // Style the series
                    series.Fill.Color = Color.FromArgb(91, 155, 213); // Blue
                    series.Border.Fill.Color = Color.FromArgb(91, 155, 213);
                    
                    // Format axis
                    chart.XAxis.Format = "dd-MMM-yy HH:mm";
                    chart.XAxis.TickLabelPosition = eTickLabelPosition.Low;
                    chart.XAxis.Font.Size = 8;
                    //chart.XAxis.Orientation = eAxisOrientation.
                    chart.YAxis.Format = "#,##0";
                    chart.YAxis.MajorGridlines.Fill.Color = Color.LightGray;
                    
                    // Style the chart
                    chart.Style = eChartStyle.Style2;
                    chart.Legend.Position = eLegendPosition.Bottom;
                }
                else
                {
                    _logger.LogDebug("Updating existing portfolio value chart");
                    
                    // Get existing chart
                    var chart = sheet.Drawings.FirstOrDefault(d => d.Name == "PortfolioChart") as ExcelLineChart;
                    if (chart != null)
                    {
                        // Update the data range
                        var lastRow = sheet.Dimension?.End.Row ?? 2; // Default to 2 if null
                        var currencyValues = sheet.Cells[2, 3, lastRow, 3]; // Currency values (column C)
                        var timestamps = sheet.Cells[2, 1, lastRow, 1]; // Timestamps (column A)
                        
                        var series = chart.Series[0];
                        series.Series = currencyValues.FullAddress;
                        series.XSeries = timestamps.FullAddress;
                    }
                }

                _logger.LogDebug("Saving totals Excel package to temp file");
                await package.SaveAsync();
            }

            // Ensure the target directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(file.FullName)!);

            // Copy the temp file to the final destination
            _logger.LogDebug("Copying totals from temp file to final destination");
            File.Copy(tempPath, file.FullName, true);
            
            // Clean up temp file
            File.Delete(tempPath);

            if (File.Exists(file.FullName))
            {
                _logger.LogInformation("Successfully created totals Excel file at: {Path}", file.FullName);
            }
            else
            {
                _logger.LogError("Failed to verify totals file creation at: {Path}", file.FullName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export totals Excel file to {Path}", file.FullName);
            throw;
        }
    }
} 