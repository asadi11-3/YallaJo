using FluentValidation;

namespace Auth.Application.Commands.UntrustDevice;

public sealed class UntrustDeviceCommandValidator : AbstractValidator<UntrustDeviceCommand>
{
    public UntrustDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("DeviceId is required.");
    }
}
