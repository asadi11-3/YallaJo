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
}
