using System.Text.Json.Serialization;

namespace CloneEbay.Application.Common;

public sealed record PaginationMetadata(int Page, int PageSize, int TotalItems, int TotalPages);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    [property: JsonIgnore] int Page,
    [property: JsonIgnore] int PageSize,
    [property: JsonIgnore] int TotalItems)
{
    [JsonIgnore]
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    [JsonPropertyName("pagination")]
    public PaginationMetadata Pagination => new(Page, PageSize, TotalItems, TotalPages);
}
