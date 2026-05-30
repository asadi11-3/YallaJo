using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourSchedule.DeleteTourSchedule;

public sealed record DeleteTourScheduleCommand(Guid TourId, Guid ScheduleId) : ICommand;
