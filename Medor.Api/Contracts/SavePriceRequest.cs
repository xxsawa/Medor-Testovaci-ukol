namespace Medor.Api.Contracts;

/// <summary>POST body when saving the current live price or a batch of observed snapshots.</summary>
public sealed class SavePriceRequest
{
    /// <summary>Required note text (trimmed when validated); applied to each row when <see cref="Items"/> is used.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// If non-empty, these snapshots are persisted (Live page history). If null or empty, the server fetches live once.
    /// </summary>
    public List<PriceSnapshotItem>? Items { get; set; }
}
