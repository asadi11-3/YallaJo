using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.CompleteExperiment;

public sealed record CompleteExperimentCommand(Guid ExperimentId) : ICommand;
