using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class BusinessTranslation : BaseEntity
{
    private BusinessTranslation() { } // EF Core

    public Guid BusinessId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Address { get; private set; }

    public Business Business { get; private set; } = default!;
}
