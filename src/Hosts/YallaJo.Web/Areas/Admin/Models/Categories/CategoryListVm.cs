namespace YallaJo.Web.Areas.Admin.Models.Categories;

public sealed class CategoryListVm
{
    public IReadOnlyList<CategoryRowVm> Categories { get; init; } = [];
    public CreateCategoryVm Create { get; init; } = new();

    // F8 §4.7: when set, the Index renders the edit modal server-side open (PE1
    // deep links / no-JS). Edit is non-nullable so asp-for expressions stay
    // warning-free; it is only meaningful when EditId has a value.
    public Guid? EditId { get; set; }
    public UpdateCategoryVm Edit { get; set; } = new();
}
