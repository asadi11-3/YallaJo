namespace Messaging.Domain.Enums;

public enum NotificationDeliveryStatus : byte
{
    Pending   = 0,
    Succeeded = 1,
    Failed    = 2,
}
