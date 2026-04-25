namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.ViewModels;

public sealed class CategoryRowVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public Guid? ParentCategoryId { get; init; }
    public int Depth { get; init; }
}
