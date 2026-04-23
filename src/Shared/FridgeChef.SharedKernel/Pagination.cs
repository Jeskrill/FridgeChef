namespace FridgeChef.SharedKernel;

public sealed record PagedRequest(int Page = 1, int PageSize = 20)
{

    public const int MaxPageSize = 50;

    public int EffectivePageSize => Math.Clamp(PageSize, 1, MaxPageSize);

    public int EffectivePage => Math.Max(1, Page);

    public int Skip => (EffectivePage - 1) * EffectivePageSize;
}

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
