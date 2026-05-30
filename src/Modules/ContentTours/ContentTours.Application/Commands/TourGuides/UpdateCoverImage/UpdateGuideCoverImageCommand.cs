using YallaJo.SharedKernel.Application.Abstractions.Messaging;
namespace ContentTours.Application.Commands.TourGuides.UpdateCoverImage;

public sealed record UpdateGuideCoverImageCommand(string? CoverImageUrl) : ICommand;
