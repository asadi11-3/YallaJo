namespace YallaJo.Web.Areas.Content.Models.Guides;

public sealed class SpecializationResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public bool IsActive { get; init; }
}
