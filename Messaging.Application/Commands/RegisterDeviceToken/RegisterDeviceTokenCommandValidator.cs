using FluentValidation;

namespace Messaging.Application.Commands.RegisterDeviceToken;

internal sealed class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
{
    public RegisterDeviceTokenCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DeviceId).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(1000);
    }
}
