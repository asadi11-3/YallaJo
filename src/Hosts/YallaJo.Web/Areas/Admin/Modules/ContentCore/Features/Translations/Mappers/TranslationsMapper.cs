using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.Requests;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.Responses;
using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.ViewModels;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.Mappers;

public static class TranslationsMapper
{
    public static EntityTranslationRowVm ToRowVm(EntityTranslationItemResponse r) => new()
    {
        Id             = r.Id,
        OriginalText   = r.OriginalText,
        TranslatedText = r.TranslatedText,
        FromLanguage   = r.FromLanguage,
        ToLanguage     = r.ToLanguage,
        FieldName      = r.FieldName,
        Status         = r.Status,
        Confidence     = r.Confidence,
        CreatedAt      = r.CreatedAt,
    };

    public static TranslateRequest ToTranslateRequest(TranslateOnDemandVm vm) => new(
        Text:             vm.Text.Trim(),
        FromLanguageCode: vm.FromLanguageCode.Trim(),
        ToLanguageCode:   vm.ToLanguageCode.Trim());

    public static UpdateTranslationRequest ToUpdateRequest(UpdateTranslationVm vm) =>
        new(vm.TranslatedText.Trim());
}
