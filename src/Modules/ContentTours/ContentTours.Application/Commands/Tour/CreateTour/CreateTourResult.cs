namespace ContentTours.Application.Commands.Tour.CreateTour;

public sealed record CreateTourResult(Guid TourId, string Name, string Slug);
