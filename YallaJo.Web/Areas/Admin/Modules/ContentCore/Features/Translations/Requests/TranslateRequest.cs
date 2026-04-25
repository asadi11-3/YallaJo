namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Translations.Requests;

public sealed record TranslateRequest(string Text, string FromLanguageCode, string ToLanguageCode);
