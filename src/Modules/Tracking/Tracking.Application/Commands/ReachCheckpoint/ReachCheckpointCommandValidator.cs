using FluentValidation;

namespace Tracking.Application.Commands.ReachCheckpoint;

public sealed class ReachCheckpointCommandValidator : AbstractValidator<ReachCheckpointCommand>
{
    public ReachCheckpointCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.CheckpointId).NotEmpty();
        RuleFor(x => x.ReachedAt).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
