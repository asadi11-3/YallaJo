using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.SubmitTour;

public sealed record SubmitTourCommand(Guid Id, byte[] RowVersion) : ICommand;
