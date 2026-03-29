using Medor.Api.Contracts;
using Medor.Api.Validation;
using Xunit;

namespace Medor.Api.Tests;

public sealed class SavedPricesRequestValidatorTests
{
    [Fact]
    public void TryValidateSave_fails_when_note_missing()
    {
        var ok = SavedPricesRequestValidator.TryValidateSave(new SavePriceRequest { Note = "  " }, out _, out var error);
        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryValidateSave_current_when_no_items()
    {
        var ok = SavedPricesRequestValidator.TryValidateSave(
            new SavePriceRequest { Note = " x " },
            out var validated,
            out _);
        Assert.True(ok);
        Assert.IsType<SaveValidated.Current>(validated);
        var cur = (SaveValidated.Current)validated!;
        Assert.Equal("x", cur.Note);
    }

    [Fact]
    public void TryValidateSave_batch_when_items_present()
    {
        var items = new List<PriceSnapshotItem> { new() };
        var ok = SavedPricesRequestValidator.TryValidateSave(
            new SavePriceRequest { Note = "n", Items = items },
            out var validated,
            out _);
        Assert.True(ok);
        var batch = Assert.IsType<SaveValidated.Batch>(validated);
        Assert.Same(items, batch.Items);
        Assert.Equal("n", batch.Note);
    }

    [Fact]
    public void TryValidateSave_fails_when_batch_exceeds_max()
    {
        var items = Enumerable.Range(0, SavedPricesRequestValidator.MaxBatchSize + 1)
            .Select(_ => new PriceSnapshotItem())
            .ToList();
        var ok = SavedPricesRequestValidator.TryValidateSave(
            new SavePriceRequest { Note = "n", Items = items },
            out _,
            out var error);
        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryValidateUpdateNotes_fails_when_empty_items()
    {
        var ok = SavedPricesRequestValidator.TryValidateUpdateNotes(new UpdateNotesRequest { Items = [] }, out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryValidateUpdateNotes_fails_when_any_note_empty()
    {
        var ok = SavedPricesRequestValidator.TryValidateUpdateNotes(
            new UpdateNotesRequest
            {
                Items =
                [
                    new NoteUpdateItem { Id = 1, Note = "a" },
                    new NoteUpdateItem { Id = 2, Note = "  " },
                ],
            },
            out _,
            out var error);
        Assert.False(ok);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryValidateUpdateNotes_builds_updates()
    {
        var ok = SavedPricesRequestValidator.TryValidateUpdateNotes(
            new UpdateNotesRequest
            {
                Items = [new NoteUpdateItem { Id = 7, Note = "  hi  " }],
            },
            out var updates,
            out _);
        Assert.True(ok);
        Assert.NotNull(updates);
        var list = Assert.Single(updates);
        Assert.Equal(7, list.Id);
        Assert.Equal("hi", list.Note);
    }

    [Fact]
    public void TryValidateDelete_fails_when_ids_empty()
    {
        var ok = SavedPricesRequestValidator.TryValidateDelete(new DeletePricesRequest { Ids = [] }, out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryValidateDelete_returns_ids()
    {
        var ids = new List<int> { 1, 2 };
        var ok = SavedPricesRequestValidator.TryValidateDelete(new DeletePricesRequest { Ids = ids }, out var outIds, out _);
        Assert.True(ok);
        Assert.Same(ids, outIds);
    }
}
