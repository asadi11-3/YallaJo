namespace ContentCore.Presentation.Endpoints.Translation.Models;

public sealed record BatchTranslateRequest(
    IReadOnlyList<string> Texts,
    string FromLanguageCode,
    string ToLanguageCode);
