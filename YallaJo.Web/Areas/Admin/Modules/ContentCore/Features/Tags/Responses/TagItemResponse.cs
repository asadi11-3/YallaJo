namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Tags.Responses;

public sealed class TagItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
