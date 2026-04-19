using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.Responses;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Categories.Mappers;

public static class CategoriesMapper
{
    public static IReadOnlyList<CategoryRowVm> Flatten(IEnumerable<CategoryItemResponse> tree)
    {
        var flat = new List<CategoryRowVm>();
        void Walk(CategoryItemResponse node, int depth)
        {
            flat.Add(new CategoryRowVm
            {
                Id               = node.Id,
                Name             = node.Name,
                Slug             = node.Slug,
                Icon             = node.Icon,
                SortOrder        = node.SortOrder,
                IsActive         = node.IsActive,
                ParentCategoryId = node.ParentCategoryId,
                Depth            = depth,
            });
            foreach (var child in node.Children.OrderBy(c => c.SortOrder))
                Walk(child, depth + 1);
        }
        foreach (var root in tree.OrderBy(c => c.SortOrder))
            Walk(root, 0);
        return flat;
    }

    public static CategoryRowVm ToRow(CategoryItemResponse r) => new()
    {
        Id               = r.Id,
        Name             = r.Name,
        Slug             = r.Slug,
        Icon             = r.Icon,
        SortOrder        = r.SortOrder,
        IsActive         = r.IsActive,
        ParentCategoryId = r.ParentCategoryId,
    };

    public static CreateCategoryRequest ToCreateRequest(CreateCategoryVm vm) => new(
        Name:               vm.Name.Trim(),
        Slug:               string.IsNullOrWhiteSpace(vm.Slug) ? null : vm.Slug.Trim(),
        ParentCategoryId:   vm.ParentCategoryId,
        Icon:               string.IsNullOrWhiteSpace(vm.Icon) ? null : vm.Icon.Trim(),
        SortOrder:          vm.SortOrder,
        SourceLanguageCode: string.IsNullOrWhiteSpace(vm.SourceLanguageCode) ? null : vm.SourceLanguageCode.Trim());

    public static UpdateCategoryRequest ToUpdateRequest(UpdateCategoryVm vm) => new(
        Name:               vm.Name.Trim(),
        Slug:               vm.Slug.Trim(),
        ParentCategoryId:   vm.ParentCategoryId,
        Icon:               string.IsNullOrWhiteSpace(vm.Icon) ? null : vm.Icon.Trim(),
        SortOrder:          vm.SortOrder,
        SourceLanguageCode: string.IsNullOrWhiteSpace(vm.SourceLanguageCode) ? null : vm.SourceLanguageCode.Trim());
}
