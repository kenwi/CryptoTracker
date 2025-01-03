using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ViewHistoricalDataCommand : Command
{
    public ViewHistoricalDataCommand() : base(
        name: "view-history",
        description: "View historical portfolio data from CSV export")
    {
        var fileOption = new Option<FileInfo>(
            name: "--file",
            description: "Path to the CSV file containing historical data")
        {
            IsRequired = true
        };

        var assetOption = new Option<string>(
            name: "--asset",
            description: "Filter by asset symbol (e.g., BTC)");

        var sourceOption = new Option<string>(
            name: "--source",
            description: "Filter by source (e.g., Binance)");

        var limitOption = new Option<int>(
            name: "--limit",
            description: "Limit the number of entries shown",
            getDefaultValue: () => 100);

        var reverseOption = new Option<bool>(
            name: "--reverse",
            description: "Show entries in reverse chronological order",
            getDefaultValue: () => false);

        AddOption(fileOption);
        AddOption(assetOption);
        AddOption(sourceOption);
        AddOption(limitOption);
        AddOption(reverseOption);

        this.SetHandler(HandleCommand, fileOption, assetOption, sourceOption, limitOption, reverseOption);
    }

    private async Task HandleCommand(
        FileInfo file,
        string? asset,
        string? source,
        int limit,
        bool reverse)
    {
        var options = new HistoricalDataViewOptions
        {
            FilePath = file.FullName,
            AssetFilter = asset,
            SourceFilter = source,
            Limit = limit,
            Reverse = reverse
        };

        // Get IHistoricalDataService from DI and execute
        using var host = CreateHost();
        var historyService = host.Services.GetRequiredService<IHistoricalDataService>();
        await historyService.ViewHistoricalDataAsync(options);
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IHistoricalDataService, HistoricalDataService>();
            })
            .Build();
    }
} 