using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourPackage : AuditableEntity
{
    private readonly List<TourPackageInclusion> _tourPackageInclusions = [];

    private TourPackage() { } // EF Core

    public Guid TourId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public int? MaxParticipants { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Tour Tour { get; private set; } = default!;
    public IReadOnlyCollection<TourPackageInclusion> TourPackageInclusions => _tourPackageInclusions.AsReadOnly();
}
