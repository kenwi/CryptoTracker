using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class DirectusService : IDirectusService
{
    private readonly HttpClient _httpClient;
    private readonly DirectusConfig _config;
    private readonly CryptoTrackingConfig _trackingConfig;
    private readonly ILogger<DirectusService> _logger;

    public DirectusService(
        IHttpClientFactory httpClientFactory,
        IOptions<DirectusConfig> config,
        IOptions<CryptoTrackingConfig> trackingConfig,
        ILogger<DirectusService> logger,
        IValueCalculationService valueCalculationService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config.Value;
        _trackingConfig = trackingConfig.Value;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
    }

    public async Task SendCoinValueAsync(string token, decimal balance, decimal price, decimal value, string source, decimal btcValue)
    {
        var data = new
        {
            token,
            balance,
            price,
            value,
            source,
            btc_value = btcValue
        };

        await SendApiRequestAsync(data, _config.CoinValuesEndpoint);
    }

    public async Task SendTotalBalanceAsync(decimal usdtBalance, decimal totalBtcValue)
    {
        var data = new
        {
            value = usdtBalance.ToString("F2", System.Globalization.CultureInfo.CreateSpecificCulture("en-US")),
            btc_value = totalBtcValue.ToString("F8", System.Globalization.CultureInfo.CreateSpecificCulture("en-US"))
        };

        await SendApiRequestAsync(data, _config.TotalBalanceEndpoint);
    }

    private async Task SendApiRequestAsync<T>(T data, string endpoint)
    {
        if (!_config.Enabled || _trackingConfig.DemoMode)
        {
            _logger.LogInformation("Directus API is {Status}. Skipping request to {Endpoint}", 
                !_config.Enabled ? "disabled" : "in demo mode", 
                endpoint);
            return;
        }

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(data),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_config.Host}/items/{endpoint}",
                content);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            if (_config.LoggingEnabled)
                _logger.LogDebug("Directus API Response: {Response}", responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending data to Directus API");
            throw;
        }
    }
}