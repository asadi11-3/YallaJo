using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.AddLanguage;

public sealed record AddTourGuideLanguageCommand(
    Guid TourGuideId,
    Guid CallerUserId,
    Guid LanguageId,
    string Proficiency) : ICommand;
