using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Specialization.ActivateSpecialization;

public sealed record ActivateSpecializationCommand(Guid Id) : ICommand;
