using FluentValidation;

namespace Finance.Application.Commands.InitiatePayment;

internal sealed class InitiatePaymentCommandValidator : AbstractValidator<InitiatePaymentCommand>
{
    public InitiatePaymentCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty()
            .WithMessage("BookingId is required.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("PaymentMethod must be one of: Card, ApplePay, GooglePay, BankTransfer.");

        RuleFor(x => x.ReturnUrl)
            .NotEmpty()
            .WithMessage("ReturnUrl is required.")
            .Must(BeValidHttpsUrl)
            .WithMessage("ReturnUrl must be a valid absolute HTTPS URL.");

        RuleFor(x => x.CallerUserId)
            .NotEmpty()
            .WithMessage("Authentication is required.");
    }

    private static bool BeValidHttpsUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }
}
