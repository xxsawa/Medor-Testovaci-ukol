using Medor.Api.Data;
using Medor.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medor.Api.Tests;

public sealed class BitcoinPriceRepositoryTests
{
    private static MedorDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MedorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new MedorDbContext(options);
    }

    [Fact]
    public async Task GetAllOrderedByFetchedDescAsync_returns_newest_first()
    {
        await using var db = CreateContext();
        db.BitcoinPrices.AddRange(
            new BitcoinPriceRecord
            {
                BtcEur = 1,
                BtcCzk = 2,
                EurCzkRate = 3,
                CnbRateValidFor = new DateOnly(2026, 1, 1),
                FetchedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Note = "old",
            },
            new BitcoinPriceRecord
            {
                BtcEur = 1,
                BtcCzk = 2,
                EurCzkRate = 3,
                CnbRateValidFor = new DateOnly(2026, 1, 1),
                FetchedAtUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                Note = "new",
            });
        await db.SaveChangesAsync();

        var sut = new BitcoinPriceRepository(db);
        var list = await sut.GetAllOrderedByFetchedDescAsync();

        Assert.Equal(2, list.Count);
        Assert.Equal("new", list[0].Note);
        Assert.Equal("old", list[1].Note);
    }

    [Fact]
    public async Task GetChartPointsOrderedAsync_orders_by_fetched_time_ascending()
    {
        await using var db = CreateContext();
        db.BitcoinPrices.AddRange(
            new BitcoinPriceRecord
            {
                BtcEur = 1,
                BtcCzk = 200m,
                EurCzkRate = 3,
                CnbRateValidFor = new DateOnly(2026, 1, 1),
                FetchedAtUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                Note = "n",
            },
            new BitcoinPriceRecord
            {
                BtcEur = 1,
                BtcCzk = 100m,
                EurCzkRate = 3,
                CnbRateValidFor = new DateOnly(2026, 1, 1),
                FetchedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Note = "n",
            });
        await db.SaveChangesAsync();

        var sut = new BitcoinPriceRepository(db);
        var points = await sut.GetChartPointsOrderedAsync();

        Assert.Equal(2, points.Count);
        Assert.Equal(100m, points[0].BtcCzk);
        Assert.Equal(200m, points[1].BtcCzk);
    }

    [Fact]
    public async Task AddRangeAsync_inserts_multiple_rows()
    {
        await using var db = CreateContext();
        var sut = new BitcoinPriceRepository(db);
        var list = new List<BitcoinPriceRecord>
        {
            new()
            {
                BtcEur = 1m,
                BtcCzk = 2m,
                EurCzkRate = 3m,
                CnbRateValidFor = new DateOnly(2026, 2, 1),
                FetchedAtUtc = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
                Note = "a",
            },
            new()
            {
                BtcEur = 1m,
                BtcCzk = 2m,
                EurCzkRate = 3m,
                CnbRateValidFor = new DateOnly(2026, 2, 1),
                FetchedAtUtc = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc),
                Note = "b",
            },
        };

        await sut.AddRangeAsync(list);

        Assert.Equal(2, await db.BitcoinPrices.CountAsync());
    }
}
