using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.ReinstateTour;

public sealed record ReinstateTourCommand(Guid Id, byte[] RowVersion) : ICommand;
