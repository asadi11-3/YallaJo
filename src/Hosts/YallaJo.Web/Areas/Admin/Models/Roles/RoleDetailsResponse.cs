namespace YallaJo.Web.Areas.Admin.Models.Roles;

public sealed class RoleDetailsResponse
{
    public Guid    Id          { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool    IsActive    { get; init; }
    public IReadOnlyList<RoleClaimItemResponse> Claims { get; init; } = [];
}


