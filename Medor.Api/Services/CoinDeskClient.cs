using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Medor.Api.Services;

/// <summary>
/// Calls CoinDesk spot latest tick API and parses BTC/EUR; retries on HTTP 429 with backoff.
/// </summary>
/// <param name="httpClient">Configured with CoinDesk base address.</param>
/// <param name="options">Market and instrument settings.</param>
/// <param name="logger">Diagnostic logger.</param>
public sealed class CoinDeskClient(
    HttpClient httpClient,
    IOptions<CoinDeskOptions> options,
    ILogger<CoinDeskClient> logger) : ICoinDeskClient
{
    private readonly CoinDeskOptions _opt = options.Value;

    /// <inheritdoc />
    public async Task<decimal> GetBtcEurAsync(CancellationToken cancellationToken = default)
    {
        var maxAttempts = Math.Max(1, _opt.MaxRetryAttempts);
        var path =
            $"/spot/v1/latest/tick?market={Uri.EscapeDataString(_opt.Market)}&instruments={Uri.EscapeDataString(_opt.Instrument)}";

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var response = await httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (attempt >= maxAttempts)
                {
                    logger.LogError("CoinDesk returned 429 after {MaxAttempts} attempts", maxAttempts);
                    response.EnsureSuccessStatusCode();
                }

                var delay = GetRetryDelay(response, attempt);
                logger.LogWarning(
                    "CoinDesk rate limited (429), attempt {Attempt}/{Max}, waiting {Delay}s",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = doc.RootElement;
            if (!root.TryGetProperty("Data", out var data) ||
                !data.TryGetProperty(_opt.Instrument, out var tick) ||
                !tick.TryGetProperty("PRICE", out var priceEl))
            {
                logger.LogError("Unexpected CoinDesk response shape");
                throw new InvalidOperationException("CoinDesk API returned unexpected JSON.");
            }

            return priceEl.GetDecimal();
        }

        throw new InvalidOperationException("CoinDesk request did not complete.");
    }

    /// <summary>Uses Retry-After header when present; otherwise scales delay by attempt number.</summary>
    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } d && d > TimeSpan.Zero)
            return Cap(d);
        if (response.Headers.RetryAfter?.Date is { } dt)
        {
            var until = dt - DateTimeOffset.UtcNow;
            if (until > TimeSpan.Zero)
                return Cap(until);
        }

        return TimeSpan.FromSeconds(Math.Min(30, 2 * attempt));
    }

    /// <summary>Caps retry delay at 60 seconds.</summary>
    private static TimeSpan Cap(TimeSpan d) => d > TimeSpan.FromSeconds(60) ? TimeSpan.FromSeconds(60) : d;
}
