using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.PromoBlock.UpdatePromoBlock;

/// <summary>
/// Updates the editable content of a promotional placement, addressed by its
/// stable <see cref="PlacementKey"/> (e.g. "MyProfile.RightRail.Top").
/// Image upload is handled separately by <c>SetPromoBlockImageCommand</c>.
/// </summary>
public sealed record UpdatePromoBlockCommand(
    string PlacementKey,
    string Title,
    string? Description,
    string? ButtonText,
    string? ButtonUrl,
    string? BadgeText,
    string? IconName,
    bool IsActive,
    int SortOrder,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt) : ICommand<UpdatePromoBlockResult>;
