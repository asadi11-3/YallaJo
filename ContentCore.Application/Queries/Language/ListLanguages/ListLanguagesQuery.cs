using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.Language.ListLanguages;

public sealed record LanguageDto(
    Guid Id,
    string Code,
    string Name,
    string NativeName,
    bool IsRtl,
    bool IsActive);

public sealed record ListLanguagesQuery(bool ActiveOnly = true) : IQuery<IReadOnlyList<LanguageDto>>;
