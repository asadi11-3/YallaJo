using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.RecordSuggestionMetric;

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
