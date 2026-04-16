namespace ContentPlaces.Application.Queries.Business.Common;

public sealed record BusinessTranslationDto(
    Guid LanguageId,
    string Name,
    string? Description,
    string? Address);
