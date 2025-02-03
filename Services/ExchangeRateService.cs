using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Dynamic;

public class ExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly ExchangeRateConfig _config;
    private decimal? _cachedRate;
    private DateTimeOffset? _nextUpdate;

    public ExchangeRateService(
        IHttpClientFactory httpClientFactory,
        ILogger<ExchangeRateService> logger,
        IOptions<ExchangeRateConfig> config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _config = config.Value;
    }

    public async Task<decimal> GetUsdExchangeRate()
    {
        if (IsCachedRateValid())
        {
            return _cachedRate!.Value;
        }

        try
        {
            var response = await _httpClient.GetStringAsync(_config.ApiUrl);            
            var data = JObject.Parse(response) as dynamic;
            var value = data?.rates[_config.Currency]?.Value;
            var timeLastUpdate = data?.time_last_updated.Value;

            if (value is null)
            {
                _logger.LogWarning($"Failed to get {_config.Currency} exchange rate");
                return _cachedRate ?? 0;
            }

            if (timeLastUpdate is null)
            {
                _logger.LogWarning("Failed to get time_last_update");
                return _cachedRate ?? 0;
            }

            _cachedRate = (decimal?)value;
            _nextUpdate = DateTimeOffset.FromUnixTimeSeconds(timeLastUpdate).AddDays(1);
            
            _logger.LogInformation("Updated exchange rate to {Rate}. Next update at: {NextUpdate}", 
                _cachedRate, _nextUpdate);

            return _cachedRate.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching USD/{_config.Currency} exchange rate");
            return _cachedRate ?? 0;
        }
    }

    private bool IsCachedRateValid()
    {
        if (_cachedRate.HasValue && _nextUpdate.HasValue && DateTimeOffset.UtcNow < _nextUpdate.Value)
        {
            _logger.LogDebug("Returning cached exchange rate. Next update at: {NextUpdate}", _nextUpdate);
            return true;
        }
        return false;
    }
} 