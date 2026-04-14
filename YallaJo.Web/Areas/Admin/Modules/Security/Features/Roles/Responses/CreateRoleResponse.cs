namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.Responses
{
    public sealed class CreateRoleResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
