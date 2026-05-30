namespace ContentCore.Application.Queries.Language.ListLanguages;

public sealed record LanguageDto(
    Guid Id,
    string Code,
    string Name,
    string NativeName,
    bool IsRtl,
    bool IsActive);
