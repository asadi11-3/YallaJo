namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels;

public sealed class RoleClaimVm
{
    public Guid   Id         { get; init; }
    public string ClaimType  { get; init; } = string.Empty;
    public string ClaimValue { get; init; } = string.Empty;
}
