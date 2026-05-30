namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.ViewModels;

public sealed class SpecializationListVm
{
    public IReadOnlyList<SpecializationRowVm> Specializations { get; init; } = [];
    public CreateSpecializationVm Create { get; init; } = new();
    public bool ActiveOnly { get; init; }
}
