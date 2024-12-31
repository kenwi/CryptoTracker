using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Authentication;

public class BinanceService : IBinanceService
{
    private readonly BinanceRestClient _client;
    private readonly ILogger<BinanceService> _logger;
    private readonly BinanceConfig _config;

    public BinanceService(
        IOptions<BinanceConfig> config,
        ILogger<BinanceService> logger)
    {
        _logger = logger;
        _config = config.Value;
        _client = new BinanceRestClient(options =>
        {
            options.ApiCredentials = new ApiCredentials(
                _config.Secret,
                _config.ApiKey);
        });
    }

    public async Task<IEnumerable<CoinBalance>> GetDetailedBalancesAsync(IEnumerable<BinanceBalance> manualBalances)
    {
        var accountInfo = await _client.SpotApi.Account.GetAccountInfoAsync();
        if (!accountInfo.Success)
        {
            _logger.LogError("Failed to fetch Binance account info");
            return Enumerable.Empty<CoinBalance>();
        }

        var balances = accountInfo.Data.Balances
            .Concat(manualBalances)
            .Where(balance => balance.Total > 0 && IsValidSymbol(balance.Asset))
            .OrderBy(balance => balance.Asset);

        var tasks = balances.Select(async balance =>
        {
            var price = await GetPriceAsync($"{balance.Asset}USDT");
            return new CoinBalance
            {
                Asset = balance.Asset,
                Balance = balance.Total,
                Price = price.Price,
                Value = price.Price * balance.Total,
                Source = manualBalances.Any(b => b.Total == balance.Total) ? "Manual" : "Binance"
            };
        });

        var results = await Task.WhenAll(tasks);
        var totalValue = results.Sum(b => b.Value);

        _logger.LogInformation(
            "Binance balances calculated: Total Value: {TotalValue} USDT, Coin Count: {CoinCount}",
            totalValue,
            results.Length);

        return results;
    }

    public async Task<BinancePrice> GetPriceAsync(string symbol)
    {
        var result = await _client.SpotApi.ExchangeData.GetPriceAsync(symbol);
        if (!result.Success)
        {
            _logger.LogError("Failed to fetch price for {Symbol}", symbol);
            return new BinancePrice { Symbol = symbol, Price = 0 };
        }
        return result.Data;
    }

    private bool IsValidSymbol(string assetName) =>
        !_config.ExcludedSymbols.Contains(assetName);
}