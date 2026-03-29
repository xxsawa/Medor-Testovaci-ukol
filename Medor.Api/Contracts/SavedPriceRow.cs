namespace Medor.Api.Contracts;

/// <summary>
/// One persisted snapshot returned from the API (table row).
/// </summary>
/// <param name="Id">Database primary key.</param>
/// <param name="BtcEur">BTC/EUR at save time.</param>
/// <param name="BtcCzk">BTC/CZK at save time.</param>
/// <param name="EurCzkRate">EUR/CZK rate used.</param>
/// <param name="CnbRateValidFor">ČNB validity date.</param>
/// <param name="FetchedAtUtc">UTC fetch time stored with the row.</param>
/// <param name="Note">User note (required in domain rules).</param>
public sealed record SavedPriceRow(
    int Id,
    decimal BtcEur,
    decimal BtcCzk,
    decimal EurCzkRate,
    DateOnly CnbRateValidFor,
    DateTime FetchedAtUtc,
    string Note);
