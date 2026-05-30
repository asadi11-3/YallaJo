using YallaJo.SharedKernel.Domain.Event;

namespace Auth.Contracts.IntegrationEvents;

public sealed record PasswordResetTokenIssuedIntegrationEvent(
    Guid TokenId,
    Guid UserId,
    string DeliveryAddress,
    string PlainCode,
    DateTime ExpiresAt,
    PasswordResetOriginSnapshot Origin) : IntegrationEventBase;

public enum PasswordResetOriginSnapshot
{
    SelfService    = 0,
    AdminInitiated = 1,
    Reassignment   = 2,
}
