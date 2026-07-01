using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface ITourPackageRepository
    : IReadRepository<TourPackage, Guid>, IWriteRepository<TourPackage, Guid>
{
    Task<TourPackage?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);

    Task<bool> InclusionDescriptionExistsAsync(
        Guid tourPackageId,
        string description,
        CancellationToken ct);

    Task<(IReadOnlyList<TourPackageSummaryRow> Items, int TotalCount)> GetPagedSummariesAsync(
        int page,
        int pageSize,
        Guid? providerId,
        decimal? minPrice,
        decimal? maxPrice,
        string? currency,
        Guid? includeTourId,
        DateTime effectiveDateUtc,
        TourPackageSortOption sort,
        CancellationToken ct);
}

public sealed record TourPackageSummaryRow(
    Guid Id,
    Guid CreatedByUserId,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency,
    int? MaxParticipants,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int IncludedTourCount,
    DateTime CreatedAt,
    string? CoverImageUrl);

public enum TourPackageSortOption
{
    Newest = 0,
    PriceAscending = 1,
    PriceDescending = 2,
    ValidityEndingSoon = 3,
}
