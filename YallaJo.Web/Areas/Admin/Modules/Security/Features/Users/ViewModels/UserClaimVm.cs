namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.ViewModels;

public sealed class UserClaimVm
{
    public Guid Id { get; init; }
    public string ClaimType { get; init; } = string.Empty;
    public string ClaimValue { get; init; } = string.Empty;
}
