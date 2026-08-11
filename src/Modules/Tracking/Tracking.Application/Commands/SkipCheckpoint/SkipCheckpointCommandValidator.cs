using FluentValidation;

namespace Tracking.Application.Commands.SkipCheckpoint;

public sealed class SkipCheckpointCommandValidator : AbstractValidator<SkipCheckpointCommand>
{
    public SkipCheckpointCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.CheckpointId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
