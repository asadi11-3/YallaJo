namespace YallaJo.Web.Areas.Admin.Models.Users
{
    public sealed class AddUserClaimRequest
    {
        public string ClaimType { get; init; } = string.Empty;
        public string ClaimValue { get; init; } = string.Empty;
    }
}
