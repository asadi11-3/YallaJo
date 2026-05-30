using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.CreateExperiment;

public sealed class CreateExperimentCommandHandler(
    IExperimentRepository experimentRepository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateExperimentCommandHandler> logger) : ICommandHandler<CreateExperimentCommand, CreateExperimentResult>
{
    public async Task<Result<CreateExperimentResult>> Handle(CreateExperimentCommand request, CancellationToken ct)
    {
        var experiment = Experiment.Create(request.Name, request.StartsAt, request.ExpiresAt, request.TrafficPercent, request.VariantsJson);
        experimentRepository.Add(experiment);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RemoveByTagAsync("analytics:experiments", ct).ConfigureAwait(false);
        logger.LogInformation("Created analytics experiment {ExperimentId} ({Name})", experiment.Id, request.Name);
        return Result<CreateExperimentResult>.Success(new CreateExperimentResult(experiment.Id));
    }
}
