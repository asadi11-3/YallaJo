using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class Redirect : AuditableEntity, IAggregateRoot
{
    private Redirect() { } // EF Core

    public string OldUrl { get; private set; } = string.Empty;
    public string NewUrl { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int HitCount { get; private set; }
}
