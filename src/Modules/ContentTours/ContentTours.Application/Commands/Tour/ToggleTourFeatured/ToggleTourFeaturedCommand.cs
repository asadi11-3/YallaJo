using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.Tour.ToggleTourFeatured;

public sealed record ToggleTourFeaturedCommand(Guid TourId, bool IsFeatured) : ICommand;
