using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.CompleteExperiment;

public sealed class CompleteExperimentCommandHandler(
    IExperimentRepository experimentRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CompleteExperimentCommandHandler> logger) : ICommandHandler<CompleteExperimentCommand>
{
    public async Task<Result> Handle(CompleteExperimentCommand request, CancellationToken ct)
    {
        var experiment = await experimentRepository.GetByIdAsync(request.ExperimentId, ct).ConfigureAwait(false);
        if (experiment is null)
            return Result.Failure(new Error("Experiment.NotFound", "Experiment was not found."), Outcome.NotFound);

        experiment.Complete();
        experimentRepository.Update(experiment);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync("analytics:experiments", ct).ConfigureAwait(false);
        logger.LogInformation("Completed analytics experiment {ExperimentId}", request.ExperimentId);
        return Result.Success();
    }
}
