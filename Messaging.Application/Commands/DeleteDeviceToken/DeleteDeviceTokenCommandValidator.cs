using FluentValidation;

namespace Messaging.Application.Commands.DeleteDeviceToken;

internal sealed class DeleteDeviceTokenCommandValidator : AbstractValidator<DeleteDeviceTokenCommand>
{
    public DeleteDeviceTokenCommandValidator()
    {
        RuleFor(x => x.TokenId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
    }
}
