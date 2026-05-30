namespace ContentCore.Presentation.Endpoints.Language.Models;

public sealed record CreateLanguageRequest(string Code, string Name, string NativeName, bool IsRtl);
