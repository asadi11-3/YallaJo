namespace YallaJo.Web.Areas.Admin.Models.Roles
{
    public sealed class AddRoleClaimRequest
    {
        public string ClaimType { get; init; } = string.Empty;
        public string ClaimValue { get; init; } = string.Empty;
    }
}
