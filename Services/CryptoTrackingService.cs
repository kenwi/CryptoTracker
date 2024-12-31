using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Binance.Net.Objects.Models.Spot;
using Microsoft.Extensions.Options;

public class CryptoTrackingService : IHostedService
{
    private readonly IBinanceService _binanceService;
    private readonly IDirectusService? _directusService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<CryptoTrackingService> _logger;
    private readonly ManualBalancesConfig _manualBalancesConfig;
    private readonly CryptoTrackingConfig _trackingConfig;
    private readonly IDisplayService _balanceDisplayService;
    private Timer? _timer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IKeyPressHandlerService _keyPressHandler;
    private readonly ICoinGeckoService? _coinGeckoService;

    public CryptoTrackingService(
        IBinanceService binanceService,
        IDirectusService? directusService,
        IExchangeRateService exchangeRateService,
        ILogger<CryptoTrackingService> logger,
        IOptions<ManualBalancesConfig> manualBalancesConfig,
        IOptions<CryptoTrackingConfig> trackingConfig,
        IDisplayService balanceDisplayService,
        IKeyPressHandlerService keyPressHandler,
        ICoinGeckoService? coinGeckoService)
    {
        _binanceService = binanceService;
        _directusService = directusService;
        _exchangeRateService = exchangeRateService;
        _logger = logger;
        _manualBalancesConfig = manualBalancesConfig.Value;
        _trackingConfig = trackingConfig.Value;
        _balanceDisplayService = balanceDisplayService;
        _keyPressHandler = keyPressHandler;
        _coinGeckoService = coinGeckoService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
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
        _timer?.Dispose();
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            var usdToNokRate = await _exchangeRateService.GetUsdToNokRateAsync();
            var manualBalances = GetManualBalances();
            var binanceBalances = await _binanceService.GetDetailedBalancesAsync(manualBalances);

            var allBalances = new List<CoinBalance>();
            allBalances.AddRange(binanceBalances);

            if (_coinGeckoService != null)
            {
                var geckoBalances = await _coinGeckoService.GetBalancesAsync();
                allBalances.AddRange(geckoBalances);
            }

            var btcPrice = allBalances.FirstOrDefault(b => b.Asset == "BTC")?.Price ?? 0;
            _balanceDisplayService.DisplayBalances(allBalances, usdToNokRate, btcPrice);

            // Only send to Directus if service is available
            if (_directusService != null)
            {
                var totalValue = allBalances.Sum(b => b.Value);
                
                var tasks = allBalances.Select(balance => 
                    _directusService.SendCoinValueAsync(
                        balance.Asset,
                        balance.Balance,
                        balance.Price,
                        balance.Value,
                        balance.Source))
                    .Append(_directusService.SendTotalBalanceAsync(totalValue));

                await Task.WhenAll(tasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing crypto values");
        }
    }

    private List<BinanceBalance> GetManualBalances() =>
        _manualBalancesConfig.Balances
            .Select(b => new BinanceBalance { Asset = b.Asset, Available = b.Available })
            .ToList();
} 