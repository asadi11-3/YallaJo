namespace ContentTours.Application.Queries.Tour.Common;

public sealed record TourTranslationDto(
    Guid LanguageId,
    string Name,
    string? Description,
    string? ShortDescription,
    string? MeetingPoint);
