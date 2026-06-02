namespace YallaJo.Web.Areas.Admin.Models.Specializations;

public sealed class SpecializationListVm
{
    public IReadOnlyList<SpecializationRowVm> Specializations { get; init; } = [];
    public CreateSpecializationVm Create { get; init; } = new();
    public bool ActiveOnly { get; init; }
}
