using ContentTours.Application.Commands.TourSchedule.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;

public sealed record CreateTourScheduleCommand(
    Guid TourId,
    TourSchedulePattern Pattern,
    /// <summary>Required when Pattern == Weekly. Bytes 0..6, Sunday=0, Saturday=6.</summary>
    IReadOnlyList<byte>? DaysOfWeek,
    /// <summary>Required when Pattern == Custom. Each entry must be in [today, today+90].</summary>
    IReadOnlyList<DateOnly>? CustomDates,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    /// <summary>Defaults to today UTC when null.</summary>
    DateOnly? ValidFrom,
    /// <summary>Defaults to ValidFrom + 90 days when null. Capped at today UTC + 90 days regardless.</summary>
    DateOnly? ValidTo,
    bool IsActive
) : ICommand<CreateTourScheduleResult>;
