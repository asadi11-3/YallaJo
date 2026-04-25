namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.Responses;

public sealed class TranslateTextResponse
{
    public string OriginalText { get; init; } = string.Empty;
    public string TranslatedText { get; init; } = string.Empty;
    public string FromLanguage { get; init; } = string.Empty;
    public string ToLanguage { get; init; } = string.Empty;
    public double? Confidence { get; init; }
}
