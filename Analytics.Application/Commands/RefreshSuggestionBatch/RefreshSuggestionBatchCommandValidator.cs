using FluentValidation;

namespace Analytics.Application.Commands.RefreshSuggestionBatch;

public sealed class RefreshSuggestionBatchCommandValidator : AbstractValidator<RefreshSuggestionBatchCommand>
{
    public RefreshSuggestionBatchCommandValidator()
    {
        RuleFor(x => x.SourceKind).IsInEnum();
        RuleFor(x => x.SourceId).NotEmpty();
        RuleFor(x => x.Context).IsInEnum();
    }
}
