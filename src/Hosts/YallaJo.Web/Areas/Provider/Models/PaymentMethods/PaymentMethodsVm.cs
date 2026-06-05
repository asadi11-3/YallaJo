using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models.PaymentMethods;

public sealed class PaymentMethodsVm
{
    public IReadOnlyList<PaymentMethodRowVm> Methods { get; init; } = [];
    public CreatePaymentMethodFormVm Form { get; set; } = new();

    public bool HasMethods => Methods.Count > 0;
}

public sealed record PaymentMethodRowVm(
    Guid Id,
    ProviderPaymentMethodType PaymentMethodType,
    string DisplayName,
    string AccountIdentifier,
    string? BankName,
    bool IsDefault,
    bool IsVerified);

public sealed class CreatePaymentMethodFormVm
{
    [Required]
    [Display(Name = "Payment method type")]
    public ProviderPaymentMethodType PaymentMethodType { get; set; } = ProviderPaymentMethodType.BankAccount;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Account identifier (IBAN / wallet number)")]
    public string AccountIdentifier { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Bank name (optional)")]
    public string? BankName { get; set; }

    [Display(Name = "Set as default")]
    public bool IsDefault { get; set; }
}
