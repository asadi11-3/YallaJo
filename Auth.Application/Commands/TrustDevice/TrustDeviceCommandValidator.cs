using FluentValidation;

namespace Auth.Application.Commands.TrustDevice;

public sealed class TrustDeviceCommandValidator : AbstractValidator<TrustDeviceCommand>
{
    public TrustDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("DeviceId is required.");
    }
}
