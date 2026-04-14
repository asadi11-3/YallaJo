namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Users.Responses
{
    public sealed class UserItemResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public IReadOnlyList<string> Roles { get; init; } = [];
    }
}
