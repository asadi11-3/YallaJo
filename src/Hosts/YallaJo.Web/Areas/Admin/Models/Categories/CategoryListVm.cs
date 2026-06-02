namespace YallaJo.Web.Areas.Admin.Models.Categories;

public sealed class CategoryListVm
{
    public IReadOnlyList<CategoryRowVm> Categories { get; init; } = [];
    public CreateCategoryVm Create { get; init; } = new();
}
