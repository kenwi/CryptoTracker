public class CoinGeckoAsset
{
    public string AssetName { get; set; } = string.Empty;
    public string CoinGeckoId { get; set; } = string.Empty;
    public DateTime TotalDate { get; set; }
    public decimal InitialTotal { get; set; }
    public decimal TokensPerDay { get; set; }
}
