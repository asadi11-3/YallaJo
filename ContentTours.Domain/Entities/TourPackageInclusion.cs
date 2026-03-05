using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourPackageInclusion : BaseEntity
{
    private TourPackageInclusion() { } // EF Core

    public Guid TourPackageId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public TourPackage TourPackage { get; private set; } = default!;
}
