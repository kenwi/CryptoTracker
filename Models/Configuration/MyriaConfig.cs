public class MyriaConfig
{
    public DateTime TotalDate { get; set; }
    public decimal InitialTotal { get; set; }
    public int TokensPerDay { get; set; }
    public string PriceApiUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
} 