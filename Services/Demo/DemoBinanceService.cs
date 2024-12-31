using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Authentication;

public class DemoBinanceService : IBinanceService
{
    private readonly BinanceRestClient _client;
    private readonly ILogger<DemoBinanceService> _logger;
    private readonly IReadOnlyList<BinanceBalance> _demoBalances;

    public DemoBinanceService(
        IOptions<BinanceConfig> config,
        ILogger<DemoBinanceService> logger,
        DemoBalanceGenerator generator)
    {
        _logger = logger;
        _client = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(
                config.Value.Secret,
                config.Value.ApiKey);
        });
        _demoBalances = generator.GenerateManualBalances().ToList();
    }

    public async Task<IEnumerable<CoinBalance>> GetDetailedBalancesAsync(IEnumerable<BinanceBalance> manualBalances)
    {
        var tasks = _demoBalances.Select(async balance =>
        {
            var price = await GetPriceAsync($"{balance.Asset}USDT");
            return new CoinBalance
            {
                Asset = balance.Asset,
                Balance = balance.Total,
                Price = price.Price,
                Value = price.Price * balance.Total,
                Source = "Demo"
            };
        });

        return await Task.WhenAll(tasks);
    }

    public async Task<BinancePrice> GetPriceAsync(string symbol)
    {
        try
        {
            var price = await _client.SpotApi.ExchangeData.GetPriceAsync(symbol);
            if (!price.Success)
            {
                _logger.LogWarning("Failed to get price for {Symbol}", symbol);
                return new BinancePrice { Price = 0 };
            }
            return price.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching price for {Symbol}", symbol);
            return new BinancePrice { Price = 0 };
        }
    }
} 