using System.Net;
using System.Text;
using Medor.Api.Services;
using Medor.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Medor.Api.Tests;

public sealed class CnbExchangeRateClientTests
{
    [Fact]
    public async Task GetEurCzkAsync_deserializes_eur_row_and_divides_rate_by_amount()
    {
        const string body = """
            {"rates":[{"currencyCode":"EUR","amount":1,"rate":25.5,"validFor":"2026-03-15"},{"currencyCode":"USD","amount":1,"rate":22,"validFor":"2026-03-15"}]}
            """;

        var handler = new StubHttpMessageHandler(req =>
        {
            Assert.Contains("lang=EN", req.RequestUri!.ToString(), StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        });
        using var http = new HttpClient(handler);
        var opt = Options.Create(new CnbOptions { DailyRatesUrl = "https://api.cnb.cz/cnbapi/exrates/daily" });
        var sut = new CnbExchangeRateClient(http, opt, NullLogger<CnbExchangeRateClient>.Instance);

        var (eurCzk, validFor) = await sut.GetEurCzkAsync(CancellationToken.None);

        Assert.Equal(25.5m, eurCzk);
        Assert.Equal(new DateOnly(2026, 3, 15), validFor);
    }

    [Fact]
    public async Task GetEurCzkAsync_amount_not_one_still_yields_czk_per_one_eur()
    {
        const string body = """
            {"rates":[{"currencyCode":"EUR","amount":100,"rate":2550,"validFor":"2026-03-15"}]}
            """;

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
        using var http = new HttpClient(handler);
        var opt = Options.Create(new CnbOptions { DailyRatesUrl = "https://api.cnb.cz/cnbapi/exrates/daily" });
        var sut = new CnbExchangeRateClient(http, opt, NullLogger<CnbExchangeRateClient>.Instance);

        var (eurCzk, _) = await sut.GetEurCzkAsync(CancellationToken.None);

        Assert.Equal(25.5m, eurCzk);
    }
}
