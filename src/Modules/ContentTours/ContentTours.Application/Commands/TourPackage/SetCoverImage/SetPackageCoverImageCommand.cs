using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentTours.Application.Commands.TourPackage.SetCoverImage;

/// <summary>
/// Sets (or clears) the optional cover image URL for a tour package.
/// The URL is produced by the file-storage service after a multipart upload
/// at the endpoint layer; passing <c>null</c> clears the cover image.
/// </summary>
public sealed record SetPackageCoverImageCommand(Guid Id, string? CoverImageUrl) : ICommand;
