namespace ContentTours.Application.Queries.TourPackage.Common;

/// <remarks>
/// Public summary DTO for the <c>AllowAnonymous</c> <c>GET /packages</c> listing.
/// Follows the same clean-public-DTO principle as
/// <see cref="TourPackageDetailDto"/> (P1-007 follow-up): it intentionally
/// omits sensitive / internal fields such as:
/// <list type="bullet">
///   <item><c>CreatedByUserId</c></item>
///   <item><c>RowVersion</c> (the optimistic-concurrency token)</item>
///   <item><c>IsDeleted</c></item>
/// </list>
/// If admin/management screens later need any of these fields, a separate
/// protected management query and DTO must be introduced — admin/internal
/// fields must NOT be retro-fitted onto this public DTO.
/// </remarks>
public sealed record TourPackageSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int IncludedTourCount,
    DateTime CreatedAt);
