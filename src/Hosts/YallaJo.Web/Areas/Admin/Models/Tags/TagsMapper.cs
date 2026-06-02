using YallaJo.Web.Areas.Admin.Models.Tags;
using YallaJo.Web.Areas.Admin.Models.Tags;
using YallaJo.Web.Areas.Admin.Models.Tags;

namespace YallaJo.Web.Areas.Admin.Models.Tags;

public static class TagsMapper
{
    public static TagRowVm ToRowVm(TagItemResponse r) => new()
    {
        Id       = r.Id,
        Name     = r.Name,
        Slug     = r.Slug,
        IsActive = r.IsActive,
    };

    public static CreateTagRequest ToCreateRequest(CreateTagVm vm) =>
        new(Name: vm.Name.Trim(), Slug: vm.Slug.Trim());

    public static UpdateTagRequest ToUpdateRequest(UpdateTagVm vm) =>
        new(Name: vm.Name.Trim(), Slug: vm.Slug.Trim());
}
