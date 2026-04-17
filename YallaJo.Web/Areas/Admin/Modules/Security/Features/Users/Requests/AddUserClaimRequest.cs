namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Requests
{
    public sealed class AddUserClaimRequest
    {
        public string ClaimType { get; init; } = string.Empty;
        public string ClaimValue { get; init; } = string.Empty;
    }
}
