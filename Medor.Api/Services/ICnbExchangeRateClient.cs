namespace Medor.Api.Services;

/// <summary>
/// HTTP client abstraction for ČNB public daily rates (EUR→CZK).
/// </summary>
public interface ICnbExchangeRateClient
{
    /// <summary>Returns CZK per 1 EUR and the ČNB validity date for that row.</summary>
    Task<(decimal EurCzkRate, DateOnly ValidFor)> GetEurCzkAsync(CancellationToken cancellationToken = default);
}
