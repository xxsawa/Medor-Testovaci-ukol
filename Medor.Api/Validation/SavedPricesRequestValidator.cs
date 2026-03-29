using System.Diagnostics.CodeAnalysis;
using Medor.Api.Contracts;
using Medor.Api.Services;

namespace Medor.Api.Validation;

/// <summary>
/// Validates saved-prices API request bodies (rules shared by the controller only; no HTTP types).
/// </summary>
public static class SavedPricesRequestValidator
{
    /// <summary>Maximum snapshots accepted in one POST when <see cref="SavePriceRequest.Items"/> is used.</summary>
    public const int MaxBatchSize = 200;

    /// <summary>
    /// Validates a save request: required note; optional batch with size cap.
    /// </summary>
    public static bool TryValidateSave(
        SavePriceRequest? body,
        [NotNullWhen(true)] out SaveValidated? validated,
        [NotNullWhen(false)] out object? error)
    {
        validated = null;
        error = null;
        var note = body?.Note?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(note))
        {
            error = new { error = "Poznámka je povinná." };
            return false;
        }

        if (body?.Items is { Count: > 0 } items)
        {
            if (items.Count > MaxBatchSize)
            {
                error = new { error = $"Lze uložit nejvýše {MaxBatchSize} záznamů najednou." };
                return false;
            }

            validated = new SaveValidated.Batch(note, items);
            return true;
        }

        validated = new SaveValidated.Current(note);
        return true;
    }

    /// <summary>
    /// Validates bulk note update: non-empty item list; each note non-empty after trim.
    /// </summary>
    public static bool TryValidateUpdateNotes(
        UpdateNotesRequest? body,
        [NotNullWhen(true)] out IReadOnlyList<NoteUpdate>? updates,
        [NotNullWhen(false)] out object? error)
    {
        updates = null;
        error = null;
        if (body?.Items is null || body.Items.Count == 0)
        {
            error = new { error = "Žádné položky k uložení." };
            return false;
        }

        var list = new List<NoteUpdate>(body.Items.Count);
        foreach (var item in body.Items)
        {
            var note = item.Note?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(note))
            {
                error = new { error = "Poznámka musí být u každého záznamu vyplněna.", id = item.Id };
                return false;
            }

            list.Add(new NoteUpdate(item.Id, note));
        }

        updates = list;
        return true;
    }

    /// <summary>
    /// Validates delete body: non-empty id list.
    /// </summary>
    public static bool TryValidateDelete(
        DeletePricesRequest? body,
        [NotNullWhen(true)] out IReadOnlyList<int>? ids,
        [NotNullWhen(false)] out object? error)
    {
        ids = null;
        error = null;
        if (body?.Ids is null || body.Ids.Count == 0)
        {
            error = new { error = "Vyberte záznamy ke smazání." };
            return false;
        }

        ids = body.Ids;
        return true;
    }
}

/// <summary>Outcome of a valid <see cref="SavePriceRequest"/>.</summary>
public abstract record SaveValidated
{
    /// <summary>Server fetches live once and persists one row.</summary>
    public sealed record Current(string Note) : SaveValidated;

    /// <summary>Client-supplied snapshots with a shared note.</summary>
    public sealed record Batch(string Note, IReadOnlyList<PriceSnapshotItem> Items) : SaveValidated;
}
