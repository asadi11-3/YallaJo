using ContentCore.Application.Queries.PromoBlock.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.PromoBlock.ListPromoBlocksByKeys;

/// <summary>
/// Public read: returns only the promotional placements that are currently
/// visible (active and within their optional schedule window) for the supplied
/// placement keys. Used to render cards for normal users.
/// </summary>
public sealed record ListPromoBlocksByKeysQuery(IReadOnlyCollection<string> PlacementKeys)
    : IQuery<IReadOnlyList<PromoBlockDto>>;
