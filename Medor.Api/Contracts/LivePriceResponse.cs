namespace Medor.Api.Contracts;

/// <summary>
/// API payload for the current live BTC/EUR, derived BTC/CZK, EUR/CZK, and ČNB validity.
/// </summary>
/// <param name="BtcEur">BTC price in EUR.</param>
/// <param name="BtcCzk">BTC price in CZK (BTC/EUR × EUR/CZK).</param>
/// <param name="EurCzkRate">CZK per 1 EUR from ČNB.</param>
/// <param name="CnbRateValidFor">Date for which the ČNB EUR rate applies.</param>
/// <param name="FetchedAtUtc">UTC time when this snapshot was computed.</param>
public sealed record LivePriceResponse(
    decimal BtcEur,
    decimal BtcCzk,
    decimal EurCzkRate,
    DateOnly CnbRateValidFor,
    DateTime FetchedAtUtc);
