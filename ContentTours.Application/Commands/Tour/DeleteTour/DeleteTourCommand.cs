using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.DeleteTour;

public sealed record DeleteTourCommand(Guid Id) : ICommand;
