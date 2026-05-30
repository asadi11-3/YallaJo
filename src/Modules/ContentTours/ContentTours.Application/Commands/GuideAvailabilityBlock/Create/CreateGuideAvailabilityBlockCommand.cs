using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.GuideAvailabilityBlock.Create;

public sealed record CreateGuideAvailabilityBlockCommand(DateOnly StartDate, DateOnly EndDate, string? Reason) : ICommand<Guid>;
