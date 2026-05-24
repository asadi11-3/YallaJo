using FluentValidation;

namespace Booking.Application.Commands.JoinRequest.CreateJoinRequest;

public sealed class CreateJoinRequestCommandValidator : AbstractValidator<CreateJoinRequestCommand>
{
    /// <summary>Matches the configured max length of <c>JoinRequest.Message</c> in the EF config (1000).</summary>
    public const int MaxMessageLength = 1000;

    public CreateJoinRequestCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId is required.");

        RuleFor(x => x.ParticipantCount)
            .GreaterThanOrEqualTo(1).WithMessage("ParticipantCount must be at least 1.");

        RuleFor(x => x.Message)
            .MaximumLength(MaxMessageLength)
            .WithMessage($"Message must be at most {MaxMessageLength} characters.");
    }
}
