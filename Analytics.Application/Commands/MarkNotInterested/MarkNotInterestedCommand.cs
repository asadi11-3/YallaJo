using Analytics.Domain.Enums;
using FluentValidation;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.MarkNotInterested;

public sealed record MarkNotInterestedCommand(
    Guid UserId,
    EntityType EntityKind,
    Guid EntityId) : ICommand;

public sealed class MarkNotInterestedCommandValidator : AbstractValidator<MarkNotInterestedCommand>
{
    public MarkNotInterestedCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.EntityId).NotEmpty();
    }
}
