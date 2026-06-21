using ContentCore.Application.Queries.PromoBlock.Common;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Queries.PromoBlock.ListPromoBlocksAdmin;

/// <summary>
/// Admin read: returns every promotional placement for the supplied keys,
/// including inactive/out-of-window blocks so editors can manage them inline.
/// Authorisation is enforced at the endpoint (Promotion.Read).
/// </summary>
public sealed record ListPromoBlocksAdminQuery(IReadOnlyCollection<string> PlacementKeys)
    : IQuery<IReadOnlyList<PromoBlockDto>>;
