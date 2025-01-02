using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<DirectusConfig>(context.Configuration.GetSection("Directus"));
                services.Configure<BinanceConfig>(context.Configuration.GetSection("Binance"));
                services.Configure<ManualBalancesConfig>(context.Configuration.GetSection("ManualBalances"));
                services.Configure<MyriaConfig>(context.Configuration.GetSection("Myria"));
                services.Configure<ExchangeRateConfig>(context.Configuration.GetSection("ExchangeRate"));
                services.Configure<CryptoTrackingConfig>(context.Configuration.GetSection("CryptoTracking"));
                services.Configure<CoinGeckoConfig>(context.Configuration.GetSection("CoinGecko"));

                // Services
                services.AddHttpClient();
                services.AddSingleton<IDisplayService, BalanceDisplayService>();
                services.AddSingleton<IExchangeRateService, ExchangeRateService>();
                services.AddSingleton<IKeyPressHandlerService, KeyPressHandlerService>();
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
                    services.AddSingleton<IBinanceService, BinanceService>();
                    
                    // Only add DirectusService if config section exists and is enabled
                    var directusConfig = context.Configuration.GetSection("Directus").Get<DirectusConfig>();
                    if (directusConfig?.Enabled == true)
                    {
                        services.AddSingleton<IDirectusService, DirectusService>();
                    }
                                       
                    // Register CoinGeckoService if configured
                    var coinGeckoConfig = context.Configuration.GetSection("CoinGecko").Get<CoinGeckoConfig>();
                    if (coinGeckoConfig?.Assets.Any() == true)
                    {
                        services.AddSingleton<ICoinGeckoService, CoinGeckoService>();
                    }
                    
                    services.AddSingleton<IHostedService>(sp =>
                    {
                        var directusService = sp.GetService<IDirectusService>();
                        var coinGeckoService = sp.GetService<ICoinGeckoService>();
                        
                        return new CryptoTrackingService(
                            sp.GetRequiredService<IBinanceService>(),
                            directusService,
                            sp.GetRequiredService<IExchangeRateService>(),
                            sp.GetRequiredService<ILogger<CryptoTrackingService>>(),
                            sp.GetRequiredService<IOptions<ManualBalancesConfig>>(),
                            sp.GetRequiredService<IOptions<CryptoTrackingConfig>>(),
                            sp.GetRequiredService<IDisplayService>(),
                            sp.GetRequiredService<IKeyPressHandlerService>(),
                            coinGeckoService,
                            sp.GetRequiredService<IValueCalculationService>()
                            );
                    });
                }
            });
}

