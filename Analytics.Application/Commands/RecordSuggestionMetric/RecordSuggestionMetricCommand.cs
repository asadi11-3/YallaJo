using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using FluentValidation;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

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

public sealed class RecordSuggestionMetricCommandHandler(
    ISuggestionMetricRepository metricRepo,
    IAnalyticsUnitOfWork unitOfWork) : ICommandHandler<RecordSuggestionMetricCommand>
{
    public async Task<Result> Handle(RecordSuggestionMetricCommand request, CancellationToken ct)
    {
        var metric = SuggestionMetric.Record(
            request.BatchId,
            request.RecommendationCacheId,
            request.UserId,
            request.Position,
            request.Stage,
            request.SessionId,
            request.ExperimentVariant);

        await metricRepo.AddAsync(metric, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
