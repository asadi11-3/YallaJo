using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetHolidayCalendarByYear;

public sealed record GetHolidayCalendarByYearQuery(int Year) : IQuery<GetHolidayCalendarByYearResult>, ICacheableQuery
{
    public string CacheKey => $"ct:analytics:admin:holidays:{Year}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [$"analytics:holidays:{Year}"];
}

public sealed record GetHolidayCalendarByYearResult(IReadOnlyList<HolidayCalendarDto> Holidays);

public sealed record HolidayCalendarDto(
    Guid Id,
    string HolidayName,
    DateOnly StartDate,
    DateOnly EndDate,
    int Year,
    string? BoostRulesJson,
    bool IsActive);
