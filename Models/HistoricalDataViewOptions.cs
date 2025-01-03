public class HistoricalDataViewOptions
{
    public string FilePath { get; set; } = string.Empty;
    public string? AssetFilter { get; set; }
    public string? SourceFilter { get; set; }
    public int Limit { get; set; } = 100;
}
