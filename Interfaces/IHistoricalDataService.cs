public interface IHistoricalDataService
{
    Task ViewHistoricalDataAsync(HistoricalDataViewOptions options);
    Task ListUniqueAssetsAsync(string filePath);
}