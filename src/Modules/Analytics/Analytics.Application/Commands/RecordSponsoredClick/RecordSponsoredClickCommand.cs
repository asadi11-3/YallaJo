using Analytics.Domain.Enums;
using FluentValidation;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.RecordSponsoredClick;

public sealed record RecordSponsoredClickCommand(
    Guid BidId,
    Guid? UserId,
    string? SessionId,
    EntityType SourceKind,
    Guid SourceId,
    int Position,
    int DwellTimeSeconds) : ICommand;

public sealed class RecordSponsoredClickCommandValidator : AbstractValidator<RecordSponsoredClickCommand>
{
    public RecordSponsoredClickCommandValidator()
    {
        RuleFor(x => x.BidId).NotEmpty();
        RuleFor(x => x.SourceId).NotEmpty();
        RuleFor(x => x.Position).GreaterThan(0);
    }
}
