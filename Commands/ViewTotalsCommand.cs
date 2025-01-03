using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class ViewTotalsCommand : Command
{
    public ViewTotalsCommand() : base(
        name: "view-totals",
        description: "View historical portfolio totals from CSV export")
    {
        var fileOption = new Option<FileInfo>(
            name: "--file",
            description: "Path to the CSV file containing historical totals")
        {
            IsRequired = true
        };

        var limitOption = new Option<int>(
            name: "--limit",
            description: "Limit the number of entries shown",
            getDefaultValue: () => 100);

        var reverseOption = new Option<bool>(
            name: "--reverse",
            description: "Show entries in reverse chronological order",
            getDefaultValue: () => false);

        AddOption(fileOption);
        AddOption(limitOption);
        AddOption(reverseOption);

        this.SetHandler(HandleCommand, fileOption, limitOption, reverseOption);
    }

    private async Task HandleCommand(FileInfo file, int limit, bool reverse)
    {
        var options = new HistoricalTotalsViewOptions
        {
            FilePath = file.FullName,
            Limit = limit,
            Reverse = reverse
        };

        using var host = CreateHost();
        var historyService = host.Services.GetRequiredService<IHistoricalDataService>();
        await historyService.ViewTotalsAsync(options);
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