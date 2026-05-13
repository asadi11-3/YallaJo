namespace ContentTours.Domain.Entities;

public sealed class TourPackageTour
{
    private TourPackageTour() { } // EF Core

    public Guid TourPackageId { get; private set; }
    public Guid TourId { get; private set; }

    public TourPackage TourPackage { get; private set; } = default!;
    public Tour Tour { get; private set; } = default!;

    public static TourPackageTour Create(Guid tourPackageId, Guid tourId)
    {
        if (tourPackageId == Guid.Empty)
            throw new ArgumentException("TourPackageId is required.", nameof(tourPackageId));
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        return new TourPackageTour
        {
            TourPackageId = tourPackageId,
            TourId = tourId,
        };
    }
}
