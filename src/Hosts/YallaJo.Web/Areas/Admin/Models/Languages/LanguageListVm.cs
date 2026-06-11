namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed class LanguageListVm
{
    public IReadOnlyList<LanguageRowVm> Languages { get; init; } = [];
    public CreateLanguageVm Create { get; init; } = new();
    public bool ActiveOnly { get; init; }

    // F8 §4.7: when set, the Index renders the edit modal server-side open (PE1
    // deep links / no-JS). Edit is non-nullable so asp-for expressions stay
    // warning-free; it is only meaningful when EditId has a value.
    public Guid? EditId { get; set; }
    public UpdateLanguageVm Edit { get; set; } = new();
}
