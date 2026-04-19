namespace FridgeChef.SharedKernel;

/// <summary>
/// Pagination request parameters.
/// </summary>
public sealed record PagedRequest(int Page = 1, int PageSize = 20)
{
    /// <summary>Maximum allowed page size to prevent abuse.</summary>
    public const int MaxPageSize = 50;

    /// <summary>Validated page size (clamped between 1 and MaxPageSize).</summary>
    public int EffectivePageSize => Math.Clamp(PageSize, 1, MaxPageSize);

    /// <summary>Validated page number (minimum 1).</summary>
    public int EffectivePage => Math.Max(1, Page);

    /// <summary>Number of items to skip for SQL OFFSET.</summary>
    public int Skip => (EffectivePage - 1) * EffectivePageSize;
}

/// <summary>
/// Paginated result wrapper.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new(Array.Empty<T>(), 0, page, pageSize);
}
