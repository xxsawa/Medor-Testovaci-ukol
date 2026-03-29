namespace Medor.Api.Services;

/// <summary>
/// Configuration for CoinDesk Data API base URL, market, and instrument id (e.g. BTC-EUR).
/// </summary>
public sealed class CoinDeskOptions
{
    /// <summary>Configuration section name in appsettings.</summary>
    public const string SectionName = "CoinDesk";

    /// <summary>HTTPS base of the CoinDesk API (no trailing path).</summary>
    public string BaseUrl { get; set; } = "https://data-api.coindesk.com";

    /// <summary>Exchange/market segment for the spot tick endpoint.</summary>
    public string Market { get; set; } = "coinbase";

    /// <summary>Instrument id used in JSON paths and query (e.g. BTC-EUR).</summary>
    public string Instrument { get; set; } = "BTC-EUR";

    /// <summary>
    /// How many times to try the HTTP request when the API returns 429 Too Many Requests (minimum 1).
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 4;
}
