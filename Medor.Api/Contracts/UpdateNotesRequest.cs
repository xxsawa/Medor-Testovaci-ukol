namespace Medor.Api.Contracts;

/// <summary>Bulk note update request body.</summary>
public sealed class UpdateNotesRequest
{
    /// <summary>Per-row id and new note.</summary>
    public List<NoteUpdateItem>? Items { get; set; }
}

/// <summary>Single item in <see cref="UpdateNotesRequest.Items"/>.</summary>
public sealed class NoteUpdateItem
{
    /// <summary>Row id.</summary>
    public int Id { get; set; }

    /// <summary>New note (must be non-empty when validated).</summary>
    public string? Note { get; set; }
}
