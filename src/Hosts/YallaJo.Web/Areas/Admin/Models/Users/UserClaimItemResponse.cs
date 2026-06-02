namespace YallaJo.Web.Areas.Admin.Models.Users
{
    public sealed class UserClaimItemResponse
    {
        public Guid Id { get; init; }
        public string ClaimType { get; init; } = string.Empty;
        public string ClaimValue { get; init; } = string.Empty;
    }
}
