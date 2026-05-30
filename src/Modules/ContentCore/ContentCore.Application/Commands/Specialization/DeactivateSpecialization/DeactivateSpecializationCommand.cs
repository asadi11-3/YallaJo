using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Specialization.DeactivateSpecialization;

public sealed record DeactivateSpecializationCommand(Guid Id) : ICommand;
