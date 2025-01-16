using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;

public class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            // Run normal application
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
            return 0;
        }

        // Setup command line interface
        var rootCommand = new RootCommand("Crypto Portfolio Tracker");
        rootCommand.AddCommand(new ViewHistoricalDataCommand());
        rootCommand.AddCommand(new ListAssetsCommand());
        rootCommand.AddCommand(new ViewTotalsCommand());
        rootCommand.AddCommand(new ConvertCsvToExcelCommand());

        return await rootCommand.InvokeAsync(args);
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                ConfigurationSetup.Configure(config);
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<DirectusConfig>(context.Configuration.GetSection("Directus"));
                services.Configure<BinanceConfig>(context.Configuration.GetSection("Binance"));
                services.Configure<ManualBalancesConfig>(context.Configuration.GetSection("ManualBalances"));
                services.Configure<MyriaConfig>(context.Configuration.GetSection("Myria"));
                services.Configure<ExchangeRateConfig>(context.Configuration.GetSection("ExchangeRate"));
                services.Configure<CryptoTrackingConfig>(context.Configuration.GetSection("CryptoTracking"));
                services.Configure<ExportConfig>(context.Configuration.GetSection("Export"));
                services.Configure<CoinGeckoConfig>(context.Configuration.GetSection("CoinGecko"));

                // Services
                services.AddHttpClient();
                services.AddSingleton<IDisplayService, BalanceDisplayService>();
                services.AddSingleton<IExchangeRateService, ExchangeRateService>();
                services.AddSingleton<IKeyPressHandlerService, KeyPressHandlerService>();
                services.AddSingleton<IExportService, ExportService>();
                services.AddSingleton<IValueCalculationService, ValueCalculationService>();

                // Conditionally register services based on demo mode
                var cryptoConfig = context.Configuration.GetSection("CryptoTracking").Get<CryptoTrackingConfig>();
                if (cryptoConfig?.DemoMode == true)
                {
                    services.AddSingleton<DemoBalanceGenerator>();
                    services.AddSingleton<IBinanceService>(sp =>
                        new DemoBinanceService(
                            sp.GetRequiredService<IOptions<BinanceConfig>>(),
                            sp.GetRequiredService<ILogger<DemoBinanceService>>(),
                            sp.GetRequiredService<DemoBalanceGenerator>()));

                    services.AddSingleton<IHostedService>(sp =>
                        new DemoCryptoTrackingService(
                            sp.GetRequiredService<IBinanceService>(),
                            sp.GetRequiredService<IExchangeRateService>(),
                            sp.GetRequiredService<IDisplayService>(),
                            sp.GetRequiredService<ILogger<DemoCryptoTrackingService>>(),
                            sp.GetRequiredService<IOptions<CryptoTrackingConfig>>(),
                            sp.GetRequiredService<DemoBalanceGenerator>(),
                            sp.GetRequiredService<IKeyPressHandlerService>()));
                }
                else
                {
                    var binanceConfig = context.Configuration.GetSection("Binance").Get<BinanceConfig>();
                    if(binanceConfig is not null)
                    {
                        services.AddSingleton<IBinanceService, BinanceService>();
                    }

                    // Only add DirectusService if config section exists and is enabled
                    var directusConfig = context.Configuration.GetSection("Directus").Get<DirectusConfig>();
                    if (directusConfig?.Enabled == true)
                    {
                        services.AddSingleton<IDirectusService, DirectusService>();
                    }
                    
                    // Only add MyriaService if config section exists and is enabled
                    var myriaConfig = context.Configuration.GetSection("Myria").Get<MyriaConfig>();
                    if (myriaConfig?.Enabled == true)
                    {
                        services.AddSingleton<IMyriaService, MyriaService>();
                    }
                    
                    // Register CoinGeckoService if configured
                    var coinGeckoConfig = context.Configuration.GetSection("CoinGecko").Get<CoinGeckoConfig>();
                    if (coinGeckoConfig?.Assets.Any() == true)
                    {
                        services.AddSingleton<ICoinGeckoService, CoinGeckoService>();
                    }
                    
                    services.AddSingleton<IHostedService>(sp =>
                    {
                        // Optional services
                        var myriaService = sp.GetService<IMyriaService>();
                        var directusService = sp.GetService<IDirectusService>();
                        var coinGeckoService = sp.GetService<ICoinGeckoService>();
                        var exportService = sp.GetService<IExportService>();
                        var binanceService = sp.GetService<IBinanceService>();

                        return new CryptoTrackingService(
                            binanceService,
                            directusService,
                            sp.GetRequiredService<IExchangeRateService>(),
                            sp.GetRequiredService<ILogger<CryptoTrackingService>>(),
                            sp.GetRequiredService<IOptions<ManualBalancesConfig>>(),
                            sp.GetRequiredService<IOptions<CryptoTrackingConfig>>(),
                            sp.GetRequiredService<IDisplayService>(),
                            sp.GetRequiredService<IKeyPressHandlerService>(),
                            coinGeckoService,
                            sp.GetRequiredService<IValueCalculationService>(),
                            exportService
                            );
                    });
                }
            });
}

