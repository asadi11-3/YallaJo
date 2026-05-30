using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Specialization.DeleteSpecialization;

public sealed record DeleteSpecializationCommand(Guid Id) : ICommand;
