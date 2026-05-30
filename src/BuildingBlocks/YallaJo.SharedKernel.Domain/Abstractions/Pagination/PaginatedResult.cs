namespace YallaJo.SharedKernel.Domain.Abstractions.Pagination
{
    public class PaginatedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PaginatedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(pageSize);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);

            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public static PaginatedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
            => new([], 0, pageNumber, pageSize);
    }
}
