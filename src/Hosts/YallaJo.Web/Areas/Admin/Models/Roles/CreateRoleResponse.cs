namespace YallaJo.Web.Areas.Admin.Models.Roles
{
    public sealed class CreateRoleResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
