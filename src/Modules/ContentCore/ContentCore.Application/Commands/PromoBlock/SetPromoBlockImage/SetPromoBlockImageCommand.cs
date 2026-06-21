using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.PromoBlock.SetPromoBlockImage;

/// <summary>
/// Replaces the image of a promotional placement. The stream is validated for a
/// supported image signature (PNG / JPEG / WEBP), uploaded to storage, and the
/// resulting URL is persisted on the block.
/// </summary>
public sealed record SetPromoBlockImageCommand(
    string PlacementKey,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize) : ICommand<SetPromoBlockImageResult>;
