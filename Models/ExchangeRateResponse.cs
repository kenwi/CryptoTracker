using System.Text.Json.Serialization;

public class ExchangeRateResponse
{
    [JsonPropertyName("rates")]
    public RateData? Rates { get; set; }

    [JsonPropertyName("time_last_updated")]
    public long TimeLastUpdated { get; set; }
} 