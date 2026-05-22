using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

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

public sealed class SubmitOnboardingResponsesCommandHandler(
    IInteractionIngestQueue queue,
    IUserExcludedEntityRepository excludedRepo,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<SubmitOnboardingResponsesCommandHandler> logger) : ICommandHandler<SubmitOnboardingResponsesCommand>
{
    public async Task<Result> Handle(SubmitOnboardingResponsesCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Deduplicate responses — if same entity appears multiple times, last wins
        var deduped = request.Responses
            .GroupBy(r => (r.EntityKind, r.EntityId))
            .Select(g => g.Last())
            .ToList();

        foreach (var response in deduped)
        {
            if (response.Interested)
            {
                // Record as Bookmark interaction (weight 2.5 handled by profile updater)
                _ = queue.TryEnqueue(new Models.InteractionEnvelope(
                    request.UserId, null, response.EntityKind, response.EntityId,
                    InteractionType.Bookmark, now, null, null));
            }
            else
            {
                // Record NotInterested interaction (weight -2.0 handled by profile updater)
                _ = queue.TryEnqueue(new Models.InteractionEnvelope(
                    request.UserId, null, response.EntityKind, response.EntityId,
                    InteractionType.NotInterested, now, null, null));

                // Check if already excluded to avoid unique index violation
                var alreadyExcluded = await excludedRepo.IsExcludedAsync(
                    request.UserId, response.EntityKind, response.EntityId, ct).ConfigureAwait(false);

                if (!alreadyExcluded)
                {
                    var exclusion = UserExcludedEntity.Create(request.UserId, response.EntityKind, response.EntityId, now.AddDays(90));
                    await excludedRepo.AddAsync(exclusion, ct).ConfigureAwait(false);
                }
            }
        }

        // Save exclusions
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        // Invalidate caches
        await cache.RemoveByTagAsync($"analytics:prefs:{request.UserId:N}", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync($"analytics:recs:{request.UserId:N}", ct).ConfigureAwait(false);

        logger.LogInformation("Processed {Count} onboarding responses for user {UserId}", request.Responses.Count, request.UserId);
        return Result.Success();
    }
}
