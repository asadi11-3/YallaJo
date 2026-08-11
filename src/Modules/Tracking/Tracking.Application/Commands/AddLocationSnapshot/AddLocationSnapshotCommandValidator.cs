using FluentValidation;

namespace Tracking.Application.Commands.AddLocationSnapshot;

public sealed class AddLocationSnapshotCommandValidator : AbstractValidator<AddLocationSnapshotCommand>
{
    public AddLocationSnapshotCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(x => x.Accuracy).GreaterThanOrEqualTo(0d);
        RuleFor(x => x.CapturedAt).NotEmpty();
    }
}
