namespace ContentCore.Application.Commands.Language.UpdateLanguage;

public sealed record UpdateLanguageResult(Guid Id, string Name, string NativeName, bool IsRtl, bool IsActive);
