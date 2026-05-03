using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourPackageInclusion : BaseEntity
{
    private TourPackageInclusion() { }

    public Guid TourPackageId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public TourPackage TourPackage { get; private set; } = default!;

    public static TourPackageInclusion Create(
        Guid tourPackageId,
        string description,
        int sortOrder)
    {
        if (tourPackageId == Guid.Empty)
            throw new ArgumentException("TourPackageId is required", nameof(tourPackageId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (sortOrder < 0)
            throw new ArgumentException("SortOrder must be >= 0", nameof(sortOrder));

        return new TourPackageInclusion
        {
            Id = Guid.CreateVersion7(),
            TourPackageId = tourPackageId,
            Description = description.Trim(),
            SortOrder = sortOrder
        };
    }

    public void Update(string description, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (sortOrder < 0)
            throw new ArgumentException("SortOrder must be >= 0", nameof(sortOrder));

        Description = description.Trim();
        SortOrder = sortOrder;
    }
}
