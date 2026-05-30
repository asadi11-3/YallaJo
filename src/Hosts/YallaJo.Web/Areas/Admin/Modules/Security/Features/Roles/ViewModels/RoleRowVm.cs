namespace YallaJo.Web.Areas.Admin.Modules.Security.Features.Roles.ViewModels
{
    public sealed class RoleRowVm
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; }
    }
}
