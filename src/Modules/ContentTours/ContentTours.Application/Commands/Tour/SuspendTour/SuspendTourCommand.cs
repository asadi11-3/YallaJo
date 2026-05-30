using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.SuspendTour;

public sealed record SuspendTourCommand(Guid Id, byte[] RowVersion, string Reason) : ICommand;
