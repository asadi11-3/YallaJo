namespace YallaJo.Web.Areas.Admin.Models.Translations;

public sealed class TranslationListVm
{
    public TranslationFilterVm Filter { get; init; } = new();
    public bool HasFilter { get; init; }
    public IReadOnlyList<EntityTranslationRowVm> Translations { get; init; } = [];
    public TranslateOnDemandVm OnDemand { get; init; } = new();
}
