namespace Medor.Api.Contracts;

/// <summary>DELETE body: ids of rows to remove.</summary>
public sealed class DeletePricesRequest
{
    /// <summary>Primary keys to delete.</summary>
    public List<int>? Ids { get; set; }
}
