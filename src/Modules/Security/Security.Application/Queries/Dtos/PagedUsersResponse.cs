using System.Text.Json.Serialization;

namespace Security.Application.Queries.Dtos;

// [method: JsonConstructor] marks the primary positional constructor as the one
// System.Text.Json must use. Without it, the presence of the secondary convenience
// constructor below makes STJ unable to choose a constructor and it throws
// "Deserialization of types without a parameterless constructor, a singular
// parameterized constructor, or a parameterized constructor annotated with
// JsonConstructorAttribute is not supported." when the cached
// Result<PagedUsersResponse> is rehydrated.
[method: JsonConstructor]
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
