using Analytics.Domain.Enums;
using FluentValidation;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.SubmitOnboardingResponses;

public sealed record OnboardingResponseItem(EntityType EntityKind, Guid EntityId, bool Interested);

public sealed record SubmitOnboardingResponsesCommand(
    Guid UserId,
    IReadOnlyList<OnboardingResponseItem> Responses) : ICommand;

public sealed class SubmitOnboardingResponsesCommandValidator : AbstractValidator<SubmitOnboardingResponsesCommand>
{
    public SubmitOnboardingResponsesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Responses).NotEmpty().Must(r => r.Count <= 20).WithMessage("Maximum 20 responses allowed.");
        RuleForEach(x => x.Responses).ChildRules(item =>
        {
            item.RuleFor(x => x.EntityId).NotEmpty();
        });
    }
}
