using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Language : AuditableEntity, IAggregateRoot
{
    private Language() { } // EF Core

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NativeName { get; private set; } = string.Empty;
    public bool IsRtl { get; private set; }
    public bool IsActive { get; private set; } = true;
}
