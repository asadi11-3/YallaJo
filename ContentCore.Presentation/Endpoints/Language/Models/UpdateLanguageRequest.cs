namespace ContentCore.Presentation.Endpoints.Language.Models;

public sealed record UpdateLanguageRequest(string Name, string NativeName, bool IsRtl, bool IsActive);
