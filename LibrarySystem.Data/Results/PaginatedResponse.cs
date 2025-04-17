namespace LibrarySystem.Data.Results;

public class PaginatedResponse<T>(int TotalCount, IEnumerable<T> Items)
{
    public int TotalCount { get; } = TotalCount;
    public IEnumerable<T> Items { get; } = Items;
}
