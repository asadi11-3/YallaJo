using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class BusinessAmenity : BaseEntity
{
    private BusinessAmenity() { } // EF Core

    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }

    public Business Business { get; private set; } = default!;

    public static BusinessAmenity Create(
        Guid businessId,
        string name,
        string? icon,
        int sortOrder)
    {
        return new BusinessAmenity
        {
            Id = Guid.CreateVersion7(),
            BusinessId = businessId,
            Name = name.Trim(),
            Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim(),
            SortOrder = sortOrder
        };
    }
}
