namespace YallaJo.Web.Areas.Provider.Models.PaymentMethods;

// Mirrors Finance.Domain.Enums.ProviderPaymentMethodType.
// Web ApiClient has no JsonStringEnumConverter, so this serializes/deserializes
// as the underlying integer value — the byte values MUST match the backend.
public enum ProviderPaymentMethodType
{
    BankAccount = 0,
    JoMoPay = 1,
    OrangeMoney = 2,
    ZainCash = 3,
}

public sealed record ProviderPaymentMethodResponse(
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

public sealed record ProviderPaymentMethodRequest(
    ProviderPaymentMethodType PaymentMethodType,
    string DisplayName,
    string AccountIdentifier,
    string? BankName,
    bool IsDefault);
