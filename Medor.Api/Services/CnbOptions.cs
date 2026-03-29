namespace Medor.Api.Services;

/// <summary>
/// Configuration for ČNB daily exchange rates JSON endpoint.
/// </summary>
public sealed class CnbOptions
{
    /// <summary>Configuration section name in appsettings.</summary>
    public const string SectionName = "Cnb";

    /// <summary>Base URL for daily rates; client appends query (e.g. lang).</summary>
    public string DailyRatesUrl { get; set; } = "https://api.cnb.cz/cnbapi/exrates/daily";
}
