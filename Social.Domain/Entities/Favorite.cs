using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

public sealed class Favorite : BaseEntity
{
    private Favorite() { } // EF Core

    public Guid UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
}
