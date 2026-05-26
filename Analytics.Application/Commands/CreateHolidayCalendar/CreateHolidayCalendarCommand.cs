using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.CreateHolidayCalendar;

public sealed record CreateHolidayCalendarCommand(
    string HolidayName,
    DateOnly StartDate,
    DateOnly EndDate,
    int Year,
    string? BoostRulesJson) : ICommand<CreateHolidayCalendarResult>;

public sealed record CreateHolidayCalendarResult(Guid Id);
