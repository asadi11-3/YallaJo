using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class PlaceTranslation : BaseEntity
{
    private PlaceTranslation() { } // EF Core

    public Guid PlaceId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Address { get; private set; }

    public Place Place { get; private set; } = default!;
}
