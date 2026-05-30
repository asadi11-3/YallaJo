using Finance.Domain.Enums;

namespace Finance.Application.ProviderPaymentMethods;

public sealed record ProviderPaymentMethodDto(
    Guid Id,
    Guid UserId,
    ProviderPaymentMethodType PaymentMethodType,
    bool IsDefault,
    bool IsVerified,
    string DisplayName,
    string AccountIdentifier,
    string? BankName,
    DateTime? VerifiedAt,
    Guid? VerifiedByAdminId);
