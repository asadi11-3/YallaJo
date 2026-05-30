using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.CreateExperiment;

public sealed record CreateExperimentCommand(
    string Name,
    DateTime StartsAt,
    DateTime ExpiresAt,
    int TrafficPercent,
    string VariantsJson) : ICommand<CreateExperimentResult>;

public sealed record CreateExperimentResult(Guid Id);
