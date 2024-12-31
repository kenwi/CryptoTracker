using System.Text.Json.Serialization;

public class RateData
{
    [JsonPropertyName("NOK")]
    public decimal NOK { get; set; }
} 