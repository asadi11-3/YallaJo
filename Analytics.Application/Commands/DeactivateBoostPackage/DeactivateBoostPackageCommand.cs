using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.DeactivateBoostPackage;

public sealed record DeactivateBoostPackageCommand(Guid BoostId) : ICommand;
