using YallaJo.Web.Areas.Admin.Models.Tags;

namespace YallaJo.Web.Areas.Admin.Models.EntityTags;

/// <summary>
/// Maps backend responses to the Entity Tags view models.
/// </summary>
public static class EntityTagsMapper
{
    public static EntityTagRowVm ToRow(this EntityTagItemResponse response)
        => new(response.TagId, response.Name, response.Slug);

    public static TagOptionVm ToOption(this TagItemResponse response)
        => new(response.Id, response.Name, response.Slug, response.IsActive);
}
