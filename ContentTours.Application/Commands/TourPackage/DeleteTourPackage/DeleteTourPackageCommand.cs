using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.DeleteTourPackage;

public sealed record DeleteTourPackageCommand(Guid Id) : ICommand;
