namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Requests
{
    public sealed class AddRoleClaimRequest
    {
        public string ClaimType { get; init; } = string.Empty;
        public string ClaimValue { get; init; } = string.Empty;
    }
}
