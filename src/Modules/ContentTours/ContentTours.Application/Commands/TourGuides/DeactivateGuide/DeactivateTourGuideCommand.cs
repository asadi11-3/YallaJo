using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourGuides.DeactivateGuide;

/// <summary>Self-deactivation by the guide themselves.</summary>
public sealed record DeactivateTourGuideCommand : ICommand;
