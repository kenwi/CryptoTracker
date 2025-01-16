using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Binance.Net.Objects.Models.Spot;
using Microsoft.Extensions.Options;
using System.Linq;

public class CryptoTrackingService : IHostedService
{
    private readonly IBinanceService? _binanceService;
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
    private readonly IValueCalculationService _valueCalculationService;
    private readonly IExportService? _exportService;

    public CryptoTrackingService(
        IBinanceService? binanceService,
        IDirectusService? directusService,
        IExchangeRateService exchangeRateService,
        ILogger<CryptoTrackingService> logger,
        IOptions<ManualBalancesConfig> manualBalancesConfig,
        IOptions<CryptoTrackingConfig> trackingConfig,
        IDisplayService balanceDisplayService,
        IKeyPressHandlerService keyPressHandler,
        ICoinGeckoService? coinGeckoService,
        IValueCalculationService valueCalculationService,
        IExportService? exportService)
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
        _valueCalculationService = valueCalculationService;
        _exportService = exportService;
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
            var usdExchangeRate = await _exchangeRateService.GetUsdExchangeRate();
            var manualBalances = GetManualBalances();

            // Try to get Binance balances with retries
            List<CoinBalance> binanceBalances = new List<CoinBalance>();
            if (_binanceService is not null)
            {
                try
                {
                    binanceBalances = (await RetryWithBackoff(
                        async () => await _binanceService.GetDetailedBalancesAsync(manualBalances),
                        maxAttempts: 3,
                        initialDelayMs: 1000))
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch Binance balances after retries");
                    // Skip this update cycle if we can't get Binance data
                    return;
                }
            }

            var allBalances = new List<CoinBalance>();
            allBalances.AddRange(binanceBalances);

            // Only add CoinGecko balances if service is available
            if (_coinGeckoService is not null)
            {
                try
                {
                    var geckoBalances = await RetryWithBackoff(
                        async () => await _coinGeckoService.GetBalancesAsync(),
                        maxAttempts: 3,
                        initialDelayMs: 1000);
                    allBalances.AddRange(geckoBalances);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch CoinGecko balances after retries");
                    // Continue with just Binance balances if CoinGecko fails
                }
            }

            // Only proceed if we have any balances
            if (!allBalances.Any())
            {
                _logger.LogWarning("No balances retrieved from any source. Skipping update.");
                return;
            }

            var btcPrice = allBalances.FirstOrDefault(b => b.Asset == "BTC")?.Price ?? 0;
            _balanceDisplayService.DisplayBalances(allBalances, usdExchangeRate, btcPrice);

            // Only export if service is available
            if(_exportService is not null)
            {
                await _exportService.ExportBalancesAsync(allBalances, usdExchangeRate, btcPrice);
            }
            
            // Only send to Directus if service is available
            if (_directusService is not null)
            {
                var tasks = allBalances.Select(balance =>
                {
                    var btcValue = _valueCalculationService.CalculateBtcValue(balance.Value, btcPrice);
                    return _directusService.SendCoinValueAsync(
                        balance.Asset,
                        balance.Balance,
                        balance.Price,
                        balance.Value,
                        balance.Source,
                        btcValue);
                });

                var totalValue = allBalances.Sum(b => b.Value);
                var totalBtcValue = _valueCalculationService.CalculateBtcValue(totalValue, btcPrice);
                tasks = tasks.Append(_directusService.SendTotalBalanceAsync(totalValue, totalBtcValue));

                await Task.WhenAll(tasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing crypto values");
        }
    }

    private async Task<T> RetryWithBackoff<T>(
        Func<Task<T>> operation,
        int maxAttempts,
        int initialDelayMs)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                    throw;

                var delayMs = initialDelayMs * Math.Pow(2, attempt - 1);
                _logger.LogWarning(ex, 
                    "Attempt {Attempt} failed. Retrying in {Delay}ms...", 
                    attempt, delayMs);
                
                await Task.Delay((int)delayMs);
            }
        }

        throw new Exception($"Failed after {maxAttempts} attempts");
    }

    private List<BinanceBalance> GetManualBalances() =>
        _manualBalancesConfig.Balances
            .Select(b => new BinanceBalance { Asset = b.Asset, Available = b.Available })
            .ToList();
} 