using Binance.Net.Objects.Models.Spot;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class DemoCryptoTrackingService : IHostedService
{
    private readonly IBinanceService _binanceService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IDisplayService _displayService;
    private readonly ILogger<DemoCryptoTrackingService> _logger;
    private readonly CryptoTrackingConfig _trackingConfig;
    private readonly DemoBalanceGenerator _demoGenerator;
    private IEnumerable<BinanceBalance> _demoBalances;
    private Timer? _timer;
    private readonly IKeyPressHandlerService _keyPressHandler;

    public DemoCryptoTrackingService(
        IBinanceService binanceService,
        IExchangeRateService exchangeRateService,
        IDisplayService displayService,
        ILogger<DemoCryptoTrackingService> logger,
        IOptions<CryptoTrackingConfig> trackingConfig,
        DemoBalanceGenerator demoGenerator,
        IKeyPressHandlerService keyPressHandler)
    {
        _binanceService = binanceService;
        _exchangeRateService = exchangeRateService;
        _displayService = displayService;
        _logger = logger;
        _trackingConfig = trackingConfig.Value;
        _demoGenerator = demoGenerator;
        _keyPressHandler = keyPressHandler;
        _demoBalances = _demoGenerator.GenerateManualBalances();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Demo Crypto Tracking Service is starting");

        _timer = new Timer(
            DoWork,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(_trackingConfig.UpdateIntervalMinutes));
        
        _ = _keyPressHandler.StartListening(
            onSpacebar: () => DoWork(null),
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Demo Crypto Tracking Service is stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            var usdToNokRate = await _exchangeRateService.GetUsdToNokRateAsync();
            var binanceBalances = await _binanceService.GetDetailedBalancesAsync(_demoBalances);

            var allBalances = binanceBalances.ToList();
            var btcPrice = allBalances.FirstOrDefault(b => b.Asset == "BTC")?.Price ?? 0;
            
            _displayService.DisplayBalances(allBalances, usdToNokRate, btcPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing demo crypto values");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
} 