using ContentCore.Application.Queries.PromoBlock.Common;

namespace ContentCore.Application.Commands.PromoBlock.UpdatePromoBlock;

/// <summary>
/// Carries the refreshed promo block back to the caller so the UI can re-render
/// the card in place without a second round trip.
/// </summary>
public sealed record UpdatePromoBlockResult(PromoBlockDto PromoBlock);
