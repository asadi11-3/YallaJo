namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.ViewModels;

public sealed class CategoryListVm
{
    public IReadOnlyList<CategoryRowVm> Categories { get; init; } = [];
    public CreateCategoryVm Create { get; init; } = new();
}
