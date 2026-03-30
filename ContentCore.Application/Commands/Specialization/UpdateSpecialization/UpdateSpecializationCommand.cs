using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Specialization.UpdateSpecialization;

public sealed record UpdateSpecializationCommand(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    bool? IsActive) : ICommand<UpdateSpecializationResult>;
