using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ListAssetsCommand : Command
{
    public ListAssetsCommand() : base(
        name: "list-assets",
        description: "List all unique assets and their sources from the historical data")
    {
        var fileOption = new Option<FileInfo>(
            name: "--file",
            description: "Path to the CSV file containing historical data")
        {
            IsRequired = true
        };

        AddOption(fileOption);
        this.SetHandler(HandleCommand, fileOption);
    }

    private async Task HandleCommand(FileInfo file)
    {
        using var host = CreateHost();
        var historyService = host.Services.GetRequiredService<IHistoricalDataService>();
        await historyService.ListUniqueAssetsAsync(file.FullName);
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