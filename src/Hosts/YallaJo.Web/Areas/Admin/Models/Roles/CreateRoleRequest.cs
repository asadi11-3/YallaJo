namespace YallaJo.Web.Areas.Admin.Models.Roles;

public sealed class CreateRoleRequest
{
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
}
