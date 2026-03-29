using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Medor.Api.Services;

/// <summary>
/// Fetches ČNB daily JSON rates and extracts EUR→CZK (CZK per 1 EUR) and validity date.
/// </summary>
/// <param name="httpClient">HTTP client (timeout set in DI).</param>
/// <param name="options">Daily rates URL.</param>
/// <param name="logger">Diagnostic logger.</param>
public sealed class CnbExchangeRateClient(
    HttpClient httpClient,
    IOptions<CnbOptions> options,
    ILogger<CnbExchangeRateClient> logger) : ICnbExchangeRateClient
{
    private readonly CnbOptions _opt = options.Value;

    /// <inheritdoc />
    public async Task<(decimal EurCzkRate, DateOnly ValidFor)> GetEurCzkAsync(
        CancellationToken cancellationToken = default)
    {
        var url = $"{_opt.DailyRatesUrl.TrimEnd('/')}?lang=EN";
        using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        // ČNB returns a large `rates` array (many currencies). We only need EUR, so we parse with
        // JsonDocument and read that row manually instead of adding DTOs for the whole payload—fewer
        // types to maintain; alternative is JsonSerializer.Deserialize into models + LINQ First(...).
        // This is more readable and maintainable than alternative (ive tried).
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("rates", out var rates) || rates.ValueKind != JsonValueKind.Array)
        {
            logger.LogError("Unexpected CNB response: missing rates array");
            throw new InvalidOperationException("CNB API returned unexpected JSON.");
        }

        foreach (var rate in rates.EnumerateArray())
        {
            if (rate.TryGetProperty("currencyCode", out var code) &&
                code.GetString() == "EUR" &&
                rate.TryGetProperty("amount", out var amountEl) &&
                rate.TryGetProperty("rate", out var rateEl) &&
                rate.TryGetProperty("validFor", out var validForEl))
            {
                var amount = amountEl.GetDecimal();
                var czkPerUnit = rateEl.GetDecimal();
                var validFor = DateOnly.Parse(validForEl.GetString()!);
                if (amount <= 0)
                    throw new InvalidOperationException("Invalid EUR amount from CNB.");
                var eurCzk = czkPerUnit / amount;
                return (eurCzk, validFor);
            }
        }

        logger.LogError("EUR rate not found in CNB daily rates");
        throw new InvalidOperationException("EUR rate not found in CNB response.");
    }
}
