namespace YallaJo.Web.Areas.Admin.Models.Specializations;

public sealed class SpecializationListVm
{
    public IReadOnlyList<SpecializationRowVm> Specializations { get; init; } = [];
    public CreateSpecializationVm Create { get; init; } = new();
    public bool ActiveOnly { get; init; }

    // F8 §4.7: when set, the Index renders the edit modal server-side open (PE1 deep links).
    public Guid? EditId { get; set; }
    public UpdateSpecializationVm Edit { get; set; } = new();
}
