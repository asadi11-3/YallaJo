using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Specialization.CreateSpecialization;

public sealed record CreateSpecializationCommand(
    string Name,
    string? Description = null,
    string? Icon = null) : ICommand<CreateSpecializationResult>;
