using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentPlaces.Application.Commands.BusinessHours.SetBusinessHours;

/// <summary>One entry per shift. Max 2 per DayOfWeek (split shifts).</summary>
public sealed record BusinessHoursEntry(
    int DayOfWeek,
    string? OpenTime,
    string? CloseTime,
    bool IsClosed);

public sealed record SetBusinessHoursCommand(
    Guid BusinessId,
    Guid ActingUserId,
    List<BusinessHoursEntry> Hours) : ICommand;
