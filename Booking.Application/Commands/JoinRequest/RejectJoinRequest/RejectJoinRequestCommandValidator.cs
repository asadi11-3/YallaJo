using FluentValidation;

namespace Booking.Application.Commands.JoinRequest.RejectJoinRequest;

public sealed class RejectJoinRequestCommandValidator : AbstractValidator<RejectJoinRequestCommand>
{
    public const int MaxReasonLength = 1000;

    public RejectJoinRequestCommandValidator()
    {
        RuleFor(x => x.JoinRequestId)
            .NotEmpty().WithMessage("JoinRequestId is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(MaxReasonLength)
            .WithMessage($"Reason must be at most {MaxReasonLength} characters.");
    }
}
