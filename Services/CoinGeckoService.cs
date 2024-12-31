using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class CoinGeckoService : ICoinGeckoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoService> _logger;
    private readonly CoinGeckoConfig _config;

    public CoinGeckoService(
        IHttpClientFactory httpClientFactory,
        ILogger<CoinGeckoService> logger,
        IOptions<CoinGeckoConfig> config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _config = config.Value;
    }

    public async Task<IEnumerable<CoinBalance>> GetBalancesAsync()
    {
        try
        {
            var coinIds = string.Join(",", _config.Assets.Select(a => a.CoinGeckoId));
            var response = await _httpClient.GetStringAsync(
                $"{_config.BaseUrl}/simple/price?ids={coinIds}&vs_currencies=usd");
            
            var priceData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(response);
            
            if (priceData == null)
            {
                _logger.LogWarning("Failed to parse CoinGecko price data");
                return [];
            }

            var balances = new List<CoinBalance>();
            foreach (var asset in _config.Assets)
            {
                if (!priceData.TryGetValue(asset.CoinGeckoId.ToLower(), out var coinData) ||
                    !coinData.TryGetValue("usd", out var price))
                {
                    _logger.LogWarning("Failed to get price for {Asset}", asset.AssetName);
                    continue;
                }

                var total = CalculateTotal(asset);
                balances.Add(new CoinBalance
                {
                    Asset = asset.AssetName,
                    Balance = total,
                    Price = price,
                    Value = price * total,
                    Source = "CoinGecko"
                });
            }

            return balances;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching CoinGecko balances");
            return [];
        }
    }

    private static decimal CalculateTotal(CoinGeckoAsset asset)
    {
        var daysSinceTotal = (DateTime.Now - asset.TotalDate).Days;
        return asset.InitialTotal + (daysSinceTotal * asset.TokensPerDay);
    }
} 