using Medor.Api.Contracts;
using Medor.Api.Services;
using Moq;
using Xunit;

namespace Medor.Api.Tests;

public sealed class LiveBitcoinPriceServiceTests
{
    [Fact]
    public async Task GetLiveAsync_multiplies_btc_eur_by_eur_czk_and_rounds_btc_czk_to_two_decimals()
    {
        var coinDesk = new Mock<ICoinDeskClient>(MockBehavior.Strict);
        var cnb = new Mock<ICnbExchangeRateClient>(MockBehavior.Strict);
        var validFor = new DateOnly(2026, 3, 15);

        coinDesk
            .Setup(x => x.GetBtcEurAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(95_123.456789m);
        cnb
            .Setup(x => x.GetEurCzkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((25.123456m, validFor));

        var sut = new LiveBitcoinPriceService(coinDesk.Object, cnb.Object);

        var result = await sut.GetLiveAsync(CancellationToken.None);

        Assert.Equal(95_123.456789m, result.BtcEur);
        Assert.Equal(25.123456m, result.EurCzkRate);
        Assert.Equal(validFor, result.CnbRateValidFor);
        var expectedBtcCzk = Math.Round(95_123.456789m * 25.123456m, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedBtcCzk, result.BtcCzk);
    }
}
