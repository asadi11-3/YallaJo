namespace YallaJo.Web.Areas.Admin.Models.Specializations;

public sealed class SpecializationItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public bool IsActive { get; init; }
}
