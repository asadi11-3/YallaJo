namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.ViewModels;

public sealed class TranslationListVm
{
    public TranslationFilterVm Filter { get; init; } = new();
    public bool HasFilter { get; init; }
    public IReadOnlyList<EntityTranslationRowVm> Translations { get; init; } = [];
    public TranslateOnDemandVm OnDemand { get; init; } = new();
}
