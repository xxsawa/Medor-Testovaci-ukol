using Medor.Api.Contracts;

namespace Medor.Api.Services;

/// <summary>
/// Loads BTC/EUR and EUR/CZK in parallel, then computes BTC/CZK (rounded).
/// </summary>
/// <param name="coinDesk">CoinDesk client.</param>
/// <param name="cnb">ČNB rates client.</param>
public sealed class LiveBitcoinPriceService(
    ICoinDeskClient coinDesk,
    ICnbExchangeRateClient cnb) : ILiveBitcoinPriceService
{
    /// <inheritdoc />
    public async Task<LivePriceResponse> GetLiveAsync(CancellationToken cancellationToken = default)
    {
        // Outbound HTTP to third parties must not use the incoming request token: client disconnect /
        // debugger abort can cancel it and surface as TaskCanceledException on HttpClient.
        using var outboundCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var outboundToken = outboundCts.Token;

        var btcEurTask = coinDesk.GetBtcEurAsync(outboundToken);
        var eurCzkTask = cnb.GetEurCzkAsync(outboundToken);
        await Task.WhenAll(btcEurTask, eurCzkTask).ConfigureAwait(false);
        var btcEur = await btcEurTask.ConfigureAwait(false);
        var (eurCzk, validFor) = await eurCzkTask.ConfigureAwait(false);
        var btcCzk = Math.Round(btcEur * eurCzk, 2, MidpointRounding.AwayFromZero);
        var fetchedAt = DateTime.UtcNow;
        return new LivePriceResponse(btcEur, btcCzk, eurCzk, validFor, fetchedAt);
    }
}
