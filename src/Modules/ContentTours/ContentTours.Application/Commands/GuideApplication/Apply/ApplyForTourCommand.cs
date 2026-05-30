using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.GuideApplication.Apply;

public sealed record ApplyForTourCommand(
    Guid TourId,
    string Message,
    string RelevantExperience,
    decimal? ProposedBasePrice,
    string? ProposedScheduleJson)
    : ICommand<Guid>;
