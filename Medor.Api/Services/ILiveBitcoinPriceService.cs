using Medor.Api.Contracts;

namespace Medor.Api.Services;

/// <summary>
/// Combines external BTC/EUR and EUR/CZK sources into a single live BTC/CZK snapshot.
/// </summary>
public interface ILiveBitcoinPriceService
{
    /// <summary>Computes BTC/EUR, EUR/CZK, and BTC/CZK for the current moment.</summary>
    Task<LivePriceResponse> GetLiveAsync(CancellationToken cancellationToken = default);
}
