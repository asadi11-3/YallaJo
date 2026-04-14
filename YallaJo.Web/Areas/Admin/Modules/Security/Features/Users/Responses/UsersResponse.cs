namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Responses;

/// <summary>Mirrors PaginatedResult&lt;UserDto&gt; from GET /api/v1/security/users.</summary>
public sealed class UserListResponse
{
    public IReadOnlyList<UserItemResponse> Items { get; init; } = [];
    public int  PageNumber      { get; init; }
    public int  PageSize        { get; init; }
    public int  TotalCount      { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage     { get; init; }
}

/// <summary>Mirrors UserDto from Security.Application.</summary>
public sealed class UserItemResponse
{
    public Guid   Id       { get; init; }
    public string Email    { get; init; } = string.Empty;
    public bool   IsActive { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}
