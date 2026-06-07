namespace YallaJo.Web.Features.Blogs.Models;

public sealed class CategoryBadgeVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Icon { get; init; }
}
