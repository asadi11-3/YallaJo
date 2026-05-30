using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.StartExperiment;

public sealed record StartExperimentCommand(Guid ExperimentId) : ICommand;
