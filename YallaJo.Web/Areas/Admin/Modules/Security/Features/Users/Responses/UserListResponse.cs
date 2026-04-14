namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Responses;

public sealed class UserListResponse
{
    public IReadOnlyList<UserItemResponse> Items { get; init; } = [];
    public int  PageNumber      { get; init; }
    public int  PageSize        { get; init; }
    public int  TotalCount      { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage     { get; init; }
}
