using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourGuides.RemoveLanguage;

public sealed record RemoveTourGuideLanguageCommand(
    Guid TourGuideId,
    Guid LanguageId) : ICommand;
