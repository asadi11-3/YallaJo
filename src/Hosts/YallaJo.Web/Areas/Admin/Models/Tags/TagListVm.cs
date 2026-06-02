namespace YallaJo.Web.Areas.Admin.Models.Tags;

public sealed class TagListVm
{
    public IReadOnlyList<TagRowVm> Tags { get; init; } = [];
    public CreateTagVm Create { get; init; } = new();
    public bool ActiveOnly { get; init; }
}
