using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Options;

public class MyriaService : IMyriaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MyriaService> _logger;
    private readonly MyriaConfig _config;
    private readonly string assetName = "MYRIA";

    public MyriaService(
        IHttpClientFactory httpClientFactory,
        ILogger<MyriaService> logger,
        IOptions<MyriaConfig> config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _config = config.Value;
    }

    public async Task<CoinBalance> GetBalanceAsync()
    {
        try
        {
            var price = await GetMyriaPriceAsync();
            var total = CalculateMyriaTotal();

            var balance = new CoinBalance
            {
                Asset = assetName,
                Balance = total,
                Price = price,
                Value = price * total,
                Source = assetName
            };

            _logger.LogInformation(
                "Myria balance calculated: Price: {Price}, Total: {Total}",
                balance.Price, balance.Balance);

            return balance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Myria balance");
            return new CoinBalance
            {
                Asset = assetName,
                Balance = 0,
                Price = 0,
                Value = 0,
                Source = assetName
            };
        }
    }

    private async Task<decimal> GetMyriaPriceAsync()
    {
        var response = await _httpClient.GetStringAsync(_config.PriceApiUrl);
        var priceData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(response);

        if (priceData == null ||
            !priceData.TryGetValue("myria", out var myriaData) ||
            !myriaData.TryGetValue("usd", out var myriaPrice))
        {
            _logger.LogWarning("Failed to parse Myria price data");
            return 0;
        }

        return myriaPrice;
    }

    private decimal CalculateMyriaTotal()
    {
        var daysSinceTotal = (DateTime.Now - _config.TotalDate).Days;
        return _config.InitialTotal + (daysSinceTotal * _config.TokensPerDay);
    }
}