using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class ProviderPaymentMethod : AuditableEntity, IAggregateRoot
{
    private ProviderPaymentMethod() { }

    public Guid UserId { get; private set; }
    public ProviderPaymentMethodType PaymentMethodType { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsVerified { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string AccountIdentifier { get; private set; } = string.Empty;
    public string? BankName { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public Guid? VerifiedByAdminId { get; private set; }

    public static Result<ProviderPaymentMethod> Create(
        Guid userId,
        ProviderPaymentMethodType paymentMethodType,
        string displayName,
        string accountIdentifier,
        string? bankName,
        bool isDefault)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<ProviderPaymentMethod>(
                new Error("ProviderPaymentMethod.UserRequired", "Provider user id is required."),
                Outcome.Invalid);
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result.Failure<ProviderPaymentMethod>(
                new Error("ProviderPaymentMethod.DisplayNameRequired", "Display name is required."),
                Outcome.Invalid);
        }

        if (string.IsNullOrWhiteSpace(accountIdentifier))
        {
            return Result.Failure<ProviderPaymentMethod>(
                new Error("ProviderPaymentMethod.AccountIdentifierRequired", "Account identifier is required."),
                Outcome.Invalid);
        }

        var method = new ProviderPaymentMethod
        {
            UserId = userId,
            PaymentMethodType = paymentMethodType,
            DisplayName = displayName.Trim(),
            AccountIdentifier = accountIdentifier.Trim(),
            BankName = string.IsNullOrWhiteSpace(bankName) ? null : bankName.Trim(),
            IsDefault = isDefault,
            IsVerified = false,
        };

        return Result.Success(method);
    }

    public Result Update(
        ProviderPaymentMethodType paymentMethodType,
        string displayName,
        string accountIdentifier,
        string? bankName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result.Failure(
                new Error("ProviderPaymentMethod.DisplayNameRequired", "Display name is required."),
                Outcome.Invalid);
        }

        if (string.IsNullOrWhiteSpace(accountIdentifier))
        {
            return Result.Failure(
                new Error("ProviderPaymentMethod.AccountIdentifierRequired", "Account identifier is required."),
                Outcome.Invalid);
        }

        PaymentMethodType = paymentMethodType;
        DisplayName = displayName.Trim();
        AccountIdentifier = accountIdentifier.Trim();
        BankName = string.IsNullOrWhiteSpace(bankName) ? null : bankName.Trim();
        IsVerified = false;
        VerifiedAt = null;
        VerifiedByAdminId = null;
        MarkUpdated();
        return Result.Success();
    }

    public Result SetAsDefault()
    {
        if (IsDefault)
        {
            return Result.Success();
        }

        IsDefault = true;
        MarkUpdated();
        return Result.Success();
    }

    public Result ClearDefault()
    {
        if (!IsDefault)
        {
            return Result.Success();
        }

        IsDefault = false;
        MarkUpdated();
        return Result.Success();
    }

    public Result Verify(Guid adminId, DateTime utcNow)
    {
        if (adminId == Guid.Empty)
        {
            return Result.Failure(
                new Error("ProviderPaymentMethod.AdminRequired", "Admin id is required."),
                Outcome.Invalid);
        }

        IsVerified = true;
        VerifiedAt = utcNow;
        VerifiedByAdminId = adminId;
        MarkUpdated();
        return Result.Success();
    }

    public Result Unverify()
    {
        IsVerified = false;
        VerifiedAt = null;
        VerifiedByAdminId = null;
        MarkUpdated();
        return Result.Success();
    }
}
