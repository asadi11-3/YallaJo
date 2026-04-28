using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourSchedule.CreateTourSchedule;

/// <summary>
/// Creates up to 7 TourSchedule rows (one per requested day-of-week) in a single transaction.
/// Weekly-only model: Booking module instantiates real date slots at booking time.
/// Returns { Created, Skipped } for idempotency visibility.
/// </summary>
public sealed record CreateTourScheduleCommand(
    Guid TourId,
    /// <summary>Days of week to schedule (byte 0–6, Sunday=0). At least one required.</summary>
    List<byte> DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    bool IsActive
) : ICommand<CreateTourScheduleResult>;
