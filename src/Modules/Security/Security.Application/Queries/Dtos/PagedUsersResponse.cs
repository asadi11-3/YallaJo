namespace Security.Application.Queries.Dtos;

public sealed record PagedUsersResponse(
    IReadOnlyList<UserDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public PagedUsersResponse(
        IReadOnlyList<UserDto> items,
        int pageNumber,
        int pageSize,
        int totalCount)
        : this(
            items,
            pageNumber,
            pageSize,
            totalCount,
            pageNumber > 1,
            pageNumber * pageSize < totalCount)
    {
    }
}
