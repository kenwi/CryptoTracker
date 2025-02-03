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
            var message = "Failed to fetch Binance account info";
            _logger.LogError(message);
            throw new Exception(message);
        }

        var timestamp = DateTime.Now;
        var accountBalances = GetAccountBalances(accountInfo.Data.Balances);
        var manualSymbols = manualBalances.Select(b => $"{b.Asset}USDT");

        // Fetch prices in parallel
        var pricesTasks = await Task.WhenAll(
            GetPricesAsync(accountBalances),
            GetPricesAsync(manualSymbols)
        );

        var accountPriceDict = pricesTasks[0].ToDictionary(p => p.Symbol, p => p.Price);
        var manualPriceDict = pricesTasks[1].ToDictionary(p => p.Symbol, p => p.Price);

        // Create balances in parallel
        var balancesTasks = await Task.WhenAll(
            Task.Run(() => CreateAccountBalances(accountInfo.Data.Balances, accountPriceDict, timestamp)),
            Task.Run(() => CreateManualBalances(manualBalances, manualPriceDict, timestamp))
        );

        var results = balancesTasks[0].Concat(balancesTasks[1]).ToList();
        var totalValue = results.Sum(b => b.Value);
        var totalTime = (DateTime.Now - timestamp).TotalMilliseconds;

        _logger.LogInformation(
            "Binance balances calculated: Total Value: {TotalValue} USDT, Coin Count: {CoinCount} in {TotalTime}ms",
            totalValue,
            results.Count,
            totalTime);

        return results;
    }

    private IEnumerable<string> GetAccountBalances(IEnumerable<BinanceBalance> balances)
    {
        return balances
            .Where(balance => balance.Total > 0 && IsValidSymbol(balance.Asset))
            .OrderBy(balance => balance.Asset)
            .Select(b => $"{b.Asset}USDT");
    }

    private IEnumerable<CoinBalance> CreateAccountBalances(
        IEnumerable<BinanceBalance> balances, 
        Dictionary<string, decimal> prices, 
        DateTime timestamp)
    {
        return balances
            .Where(b => b.Total > 0 && IsValidSymbol(b.Asset))
            .Select(balance =>
            {
                var symbol = $"{balance.Asset}USDT";
                return CreateCoinBalance(
                    timestamp,
                    balance.Asset,
                    balance.Total,
                    prices.GetValueOrDefault(symbol),
                    "Binance");
            });
    }

    private static IEnumerable<CoinBalance> CreateManualBalances(
        IEnumerable<BinanceBalance> manualBalances, 
        Dictionary<string, decimal> prices, 
        DateTime timestamp)
    {
        return manualBalances.Select(balance =>
        {
            var symbol = $"{balance.Asset}USDT";
            return CreateCoinBalance(
                timestamp,
                balance.Asset,
                balance.Total,
                prices.GetValueOrDefault(symbol),
                "Manual");
        });
    }

    private static CoinBalance CreateCoinBalance(
        DateTime timestamp,
        string asset,
        decimal balance,
        decimal price,
        string source)
    {
        return new CoinBalance
        {
            Timestamp = timestamp,
            Asset = asset,
            Balance = balance,
            Price = price,
            Value = price * balance,
            Source = source
        };
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
    
    public async Task<IEnumerable<BinancePrice>> GetPricesAsync(IEnumerable<string> symbols)
    {
        if (!symbols.Any())
        {
            return Enumerable.Empty<BinancePrice>();
        }

        var result = await _client.SpotApi.ExchangeData.GetPricesAsync(symbols);
        if (!result.Success)
        {
            _logger.LogError("Failed to fetch prices for {Symbols}", string.Join(", ", symbols));
            return Enumerable.Empty<BinancePrice>();
        }
        return result.Data;
    }

    private bool IsValidSymbol(string assetName) =>
        !_config.ExcludedSymbols.Contains(assetName);
}