namespace Medor.Api.Services;

/// <summary>
/// HTTP client abstraction for CoinDesk spot API (BTC/EUR tick).
/// </summary>
public interface ICoinDeskClient
{
    /// <summary>Fetches the latest BTC/EUR price from the configured market/instrument.</summary>
    Task<decimal> GetBtcEurAsync(CancellationToken cancellationToken = default);
}
