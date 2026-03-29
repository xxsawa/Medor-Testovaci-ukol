using System.Net;
using System.Text;
using Medor.Api.Services;
using Medor.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Medor.Api.Tests;

public sealed class CoinDeskClientTests
{
    [Fact]
    public async Task GetBtcEurAsync_reads_price_from_configured_instrument_path()
    {
        const string body = """
            {"Data":{"BTC-EUR":{"PRICE":50000.123456789}}}
            """;

        var handler = new StubHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Contains("market=coinbase", req.RequestUri!.Query, StringComparison.Ordinal);
            Assert.Contains("instruments=BTC-EUR", req.RequestUri.Query, StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://data-api.coindesk.com/") };
        var opt = Options.Create(
            new CoinDeskOptions
            {
                BaseUrl = "https://data-api.coindesk.com",
                Market = "coinbase",
                Instrument = "BTC-EUR",
                MaxRetryAttempts = 1,
            });
        var sut = new CoinDeskClient(http, opt, NullLogger<CoinDeskClient>.Instance);

        var price = await sut.GetBtcEurAsync(CancellationToken.None);

        Assert.Equal(50000.123456789m, price);
    }
}
