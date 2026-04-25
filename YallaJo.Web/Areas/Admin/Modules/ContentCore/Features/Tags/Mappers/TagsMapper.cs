using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.Responses;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.Mappers;

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
