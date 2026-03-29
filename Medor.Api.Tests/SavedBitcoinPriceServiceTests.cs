using Medor.Api.Contracts;
using Medor.Api.Data;
using Medor.Api.Persistence;
using Medor.Api.Services;
using Moq;
using Xunit;

namespace Medor.Api.Tests;

public sealed class SavedBitcoinPriceServiceTests
{
    [Fact]
    public async Task SaveCurrentAsync_trims_note_and_persists_snapshot_from_live_service()
    {
        var validFor = new DateOnly(2026, 1, 10);
        var fetched = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var snapshot = new LivePriceResponse(1.5m, 100m, 24m, validFor, fetched);

        var live = new Mock<ILiveBitcoinPriceService>(MockBehavior.Strict);
        live.Setup(x => x.GetLiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        BitcoinPriceRecord? added = null;
        var repo = new Mock<IBitcoinPriceRepository>(MockBehavior.Strict);
        repo
            .Setup(x => x.AddAsync(It.IsAny<BitcoinPriceRecord>(), It.IsAny<CancellationToken>()))
            .Callback<BitcoinPriceRecord, CancellationToken>((e, _) =>
            {
                added = e;
                e.Id = 42;
            })
            .Returns(Task.CompletedTask);

        var sut = new SavedBitcoinPriceService(repo.Object, live.Object);
        var row = await sut.SaveCurrentAsync("  my note  ");

        Assert.NotNull(added);
        Assert.Equal(1.5m, added!.BtcEur);
        Assert.Equal(100m, added.BtcCzk);
        Assert.Equal(24m, added.EurCzkRate);
        Assert.Equal(validFor, added.CnbRateValidFor);
        Assert.Equal(fetched, added.FetchedAtUtc);
        Assert.Equal("my note", added.Note);

        Assert.Equal(42, row.Id);
        Assert.Equal("my note", row.Note);
        repo.Verify(x => x.AddAsync(It.IsAny<BitcoinPriceRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveSnapshotsAsync_persists_all_with_trimmed_note_via_AddRange()
    {
        var validFor = new DateOnly(2026, 1, 10);
        var fetched1 = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var fetched2 = fetched1.AddMinutes(1);
        var items = new List<PriceSnapshotItem>
        {
            new()
            {
                BtcEur = 1m,
                BtcCzk = 2m,
                EurCzkRate = 3m,
                CnbRateValidFor = validFor,
                FetchedAtUtc = fetched1,
            },
            new()
            {
                BtcEur = 1.1m,
                BtcCzk = 2.1m,
                EurCzkRate = 3.1m,
                CnbRateValidFor = validFor,
                FetchedAtUtc = fetched2,
            },
        };

        IReadOnlyList<BitcoinPriceRecord>? added = null;
        var repo = new Mock<IBitcoinPriceRepository>(MockBehavior.Strict);
        repo
            .Setup(x => x.AddRangeAsync(It.IsAny<IReadOnlyList<BitcoinPriceRecord>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<BitcoinPriceRecord>, CancellationToken>((list, _) =>
            {
                added = list;
                var id = 100;
                foreach (var e in list)
                {
                    e.Id = id++;
                }
            })
            .Returns(Task.CompletedTask);

        var live = new Mock<ILiveBitcoinPriceService>(MockBehavior.Strict);
        var sut = new SavedBitcoinPriceService(repo.Object, live.Object);
        var rows = await sut.SaveSnapshotsAsync(items, "  batch  ");

        Assert.NotNull(added);
        Assert.Equal(2, added!.Count);
        Assert.All(added, e => Assert.Equal("batch", e.Note));
        Assert.Equal(2, rows.Count);
        Assert.Equal(100, rows[0].Id);
        repo.Verify(x => x.AddRangeAsync(It.IsAny<IReadOnlyList<BitcoinPriceRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateNotesAsync_maps_to_repository_pairs()
    {
        // Use ValueTuple in Moq setups (not named tuple types) — expression trees cannot contain tuple types in It.Is<> / It.IsAny<> the same way.
        IReadOnlyList<(int Id, string Note)>? captured = null;
        var repo = new Mock<IBitcoinPriceRepository>(MockBehavior.Strict);
        repo
            .Setup(x => x.UpdateNotesAsync(It.IsAny<IReadOnlyList<ValueTuple<int, string>>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<(int Id, string Note)>, CancellationToken>((list, _) => captured = list)
            .Returns(Task.CompletedTask);

        var live = new Mock<ILiveBitcoinPriceService>(MockBehavior.Loose);
        var sut = new SavedBitcoinPriceService(repo.Object, live.Object);

        await sut.UpdateNotesAsync(
            new List<NoteUpdate> { new(1, "  a  "), new(2, "b") },
            CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Count);
        Assert.Equal(1, captured[0].Id);
        Assert.Equal("a", captured[0].Note);
        Assert.Equal(2, captured[1].Id);
        Assert.Equal("b", captured[1].Note);
        repo.Verify(x => x.UpdateNotesAsync(It.IsAny<IReadOnlyList<ValueTuple<int, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
