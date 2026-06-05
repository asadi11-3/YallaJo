using YallaJo.Web.Areas.Admin.Models.Categories;

namespace YallaJo.Web.Areas.Admin.Models.EntityCategories;

public static class EntityCategoriesMapper
{
    public static EntityCategoryRowVm ToRow(this EntityCategoryItemResponse r)
        => new(r.CategoryId, r.Name, r.Slug);

    public static CategoryOptionVm ToOption(this CategoryItemResponse c)
        => new(c.Id, c.Name, c.Slug, c.IsActive);
}
