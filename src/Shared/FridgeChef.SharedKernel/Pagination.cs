namespace FridgeChef.SharedKernel;

// Параметры постраничного запроса.
public sealed record PagedRequest(int Page = 1, int PageSize = 20)
{
    // Максимально допустимый размер страницы.
    public const int MaxPageSize = 50;

    // Размер страницы, ограниченный допустимым диапазоном.
    public int EffectivePageSize => Math.Clamp(PageSize, 1, MaxPageSize);

    // Номер страницы, не меньше 1.
    public int EffectivePage => Math.Max(1, Page);

    // Количество элементов для пропуска (SQL OFFSET).
    public int Skip => (EffectivePage - 1) * EffectivePageSize;
}

// Обёртка для постраничного результата.
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
