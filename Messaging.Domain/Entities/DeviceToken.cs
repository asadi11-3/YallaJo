using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class DeviceToken : AuditableEntity
{
    private DeviceToken() { } // EF Core

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DevicePlatform Platform { get; private set; }
    public string? DeviceName { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastUsedAt { get; private set; }
}
