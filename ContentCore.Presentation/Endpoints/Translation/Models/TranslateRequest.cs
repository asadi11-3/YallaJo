namespace ContentCore.Presentation.Endpoints.Translation.Models;

public sealed record TranslateRequest(string Text, string FromLanguageCode, string ToLanguageCode);
