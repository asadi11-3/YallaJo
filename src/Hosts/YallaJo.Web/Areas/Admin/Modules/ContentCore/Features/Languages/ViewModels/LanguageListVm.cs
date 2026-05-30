namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.ViewModels;

public sealed class LanguageListVm
{
    public IReadOnlyList<LanguageRowVm> Languages { get; init; } = [];
    public CreateLanguageVm Create { get; init; } = new();
    public bool ActiveOnly { get; init; }
}
