public interface ICoinGeckoService
{
    Task<IEnumerable<CoinBalance>> GetBalancesAsync();
}
