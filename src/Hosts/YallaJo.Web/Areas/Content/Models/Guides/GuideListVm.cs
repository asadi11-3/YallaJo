namespace YallaJo.Web.Areas.Content.Models.Guides;

public sealed class GuideListVm
{
    public IReadOnlyList<GuideCardVm> Guides { get; init; } = [];
    public IReadOnlyList<SpecializationVm> Specializations { get; init; } = [];
    public GuidePagerVm Pager { get; init; } = new();

    public bool HasGuides => Guides.Count > 0;
}
