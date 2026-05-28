using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideAvailabilityBlock.Delete;

public sealed record DeleteGuideAvailabilityBlockCommand(Guid BlockId) : ICommand;
