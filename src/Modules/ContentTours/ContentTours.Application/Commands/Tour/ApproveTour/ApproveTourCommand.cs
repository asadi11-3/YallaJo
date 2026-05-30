using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.ApproveTour;

public sealed record ApproveTourCommand(Guid Id, byte[] RowVersion) : ICommand;
