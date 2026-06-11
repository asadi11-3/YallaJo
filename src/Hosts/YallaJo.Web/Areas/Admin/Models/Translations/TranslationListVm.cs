namespace YallaJo.Web.Areas.Admin.Models.Translations;

public sealed class TranslationListVm
{
    public TranslationFilterVm Filter { get; init; } = new();
    public bool HasFilter { get; init; }
    public IReadOnlyList<EntityTranslationRowVm> Translations { get; init; } = [];
    public TranslateOnDemandVm OnDemand { get; init; } = new();

    // F8 §4.7: when set, the Index renders the edit modal server-side open (PE1 deep links).
    public Guid? EditId { get; set; }
    public UpdateTranslationVm Edit { get; set; } = new();
    public string EditOriginal { get; set; } = string.Empty;
}
