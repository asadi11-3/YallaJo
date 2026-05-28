using FluentValidation;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.RecordSuggestionMetric;

public sealed record RecordSuggestionMetricCommand(
    Guid? BatchId,
    Guid? RecommendationCacheId,
    Guid? UserId,
    int Position,
    string Stage,
    string? SessionId,
    string? ExperimentVariant) : ICommand;

public sealed class RecordSuggestionMetricCommandValidator : AbstractValidator<RecordSuggestionMetricCommand>
{
    public RecordSuggestionMetricCommandValidator()
    {
        RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stage).NotEmpty().Must(s => s is "Impression" or "Click" or "Booking")
            .WithMessage("Stage must be Impression, Click, or Booking.");
    }
}
