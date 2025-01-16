using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CsvHelper;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ConvertCsvToExcelCommand : Command
{
    public ConvertCsvToExcelCommand() : base("convert-csv", "Convert existing CSV exports to Excel format with charts")
    {
        var valuesOption = new Option<FileInfo>(
            "--values-file",
            "Path to the values CSV file (e.g., crypto-portfolio-values.csv)")
        {
            IsRequired = true
        };
            
        var totalsOption = new Option<FileInfo>(
            "--totals-file",
            "Path to the totals CSV file (e.g., crypto-portfolio-totals.csv)")
        {
            IsRequired = true
        };
            
        var outputPathOption = new Option<string>(
            "--output-path",
            () => "exports",
            "Directory where Excel files will be saved");

        AddOption(valuesOption);
        AddOption(totalsOption);
        AddOption(outputPathOption);

        this.SetHandler(HandleCommand, valuesOption, totalsOption, outputPathOption);
    }

    private async Task HandleCommand(FileInfo valuesFile, FileInfo totalsFile, string outputPath)
    {
        using var host = CreateHost(outputPath);
        var logger = host.Services.GetRequiredService<ILogger<ConvertCsvToExcelCommand>>();
        var exportService = host.Services.GetRequiredService<IExportService>();
        var valueCalculationService = host.Services.GetRequiredService<IValueCalculationService>();

        try
        {
            if (!valuesFile.Exists)
            {
                logger.LogError("Values file not found: {Path}", valuesFile.FullName);
                return;
            }

            if (!totalsFile.Exists)
            {
                logger.LogError("Totals file not found: {Path}", totalsFile.FullName);
                return;
            }

            // Read values CSV
            var balances = new List<CoinBalance>();
            using (var reader = new StreamReader(valuesFile.FullName))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                if (!csv.Read() || !csv.ReadHeader())
                {
                    logger.LogError("Invalid values CSV file format");
                    return;
                }

                while (csv.Read())
                {
                    balances.Add(new CoinBalance
                    {
                        Timestamp = csv.GetField<DateTime>("Timestamp"),
                        Asset = csv.GetField<string>("Asset") ?? string.Empty,
                        Balance = csv.GetField<decimal>("Balance"),
                        Price = csv.GetField<decimal>("Price (USDT)"),
                        Value = csv.GetField<decimal>("Value (USDT)"),
                        Source = csv.GetField<string>("Source") ?? string.Empty
                    });
                }
            }

            // Read latest exchange rates from totals CSV
            decimal usdToNokRate = 0;
            decimal btcPrice = 0;
            using (var reader = new StreamReader(totalsFile.FullName))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                if (!csv.Read() || !csv.ReadHeader())
                {
                    logger.LogError("Invalid totals CSV file format");
                    return;
                }

                string? lastLine = null;
                while (csv.Read())
                {
                    lastLine = csv.Context?.Parser?.RawRecord ?? string.Empty;
                }

                if (!string.IsNullOrEmpty(lastLine))
                {
                    var parts = lastLine.Split(',');
                    if (parts.Length >= 3 && 
                        decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var totalUsd) &&
                        decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var totalNok))
                    {
                        usdToNokRate = totalNok / totalUsd;

                        if (parts.Length >= 4 && 
                            decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var totalBtc))
                        {
                            btcPrice = totalUsd / totalBtc;
                        }
                    }
                }
            }

            // Create Excel exports
            Directory.CreateDirectory(outputPath);

            // Configure export service through options
            var config = new ExportConfig
            {
                Enabled = true,
                Format = "xlsx",
                OutputPath = outputPath,
                ValuesFilename = Path.GetFileName(valuesFile.Name).Replace(".csv", ""),
                TotalsFilename = Path.GetFileName(totalsFile.Name).Replace(".csv", "")
            };

            // Update the export service configuration
            var optionsService = host.Services.GetRequiredService<IOptionsMonitor<ExportConfig>>();
            var currentConfig = optionsService.CurrentValue;
            currentConfig.OutputPath = outputPath;
            currentConfig.ValuesFilename = config.ValuesFilename;
            currentConfig.TotalsFilename = config.TotalsFilename;

            await exportService.ExportBalancesAsync(balances, usdToNokRate, btcPrice);
            
            logger.LogInformation("Successfully converted CSV files to Excel format in {OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to convert CSV files to Excel");
        }
    }

    private static IHost CreateHost(string outputPath)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                ConfigurationSetup.Configure(config);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(builder => builder.AddConsole());
                services.AddTransient<IExportService, ExportService>();
                services.AddTransient<IValueCalculationService, ValueCalculationService>();
                services.Configure<ExportConfig>(config =>
                {
                    config.Enabled = true;
                    config.Format = "xlsx";
                    config.OutputPath = outputPath;
                });
            })
            .Build();
    }
}