// <copyright file="ReorderFaqItemsCommandHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.ReorderFaqItems;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class ReorderFaqItemsCommandHandler(
    IFaqItemRepository faqItemRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<ReorderFaqItemsCommandHandler> logger)
    : ICommandHandler<ReorderFaqItemsCommand>
{
    public async Task<Result> Handle(ReorderFaqItemsCommand request, CancellationToken ct)
    {
        try
        {
            // Auth handled by endpoint MustHavePermissionAttribute.

            // Tracked load (no include) so Reorder mutations persist
            var entities = await faqItemRepository.GetByEntityAsync(request.EntityType, request.EntityId, ct);
            if (entities.Count == 0)
            {
                return Result.Failure(new Error("FaqItem.NotFound", "No FAQ items found for this entity."), Outcome.NotFound);
            }

            var byId = entities.ToDictionary(e => e.Id);

            // Verify all requested ids exist in the entity scope
            foreach (var item in request.Items)
            {
                if (!byId.ContainsKey(item.Id))
                {
                    return Result.Failure(
                        new Error("FaqItem.NotFound", $"FAQ item {item.Id} not found in this entity's scope."),
                        Outcome.NotFound);
                }
            }

            // Two-phase reorder: stash in a temp range to avoid uniqueness conflicts mid-update.
            // Phase 1: assign distinct large temp values
            var temp = 10_000;
            foreach (var item in request.Items)
            {
                byId[item.Id].Reorder(temp++);
            }

            // Phase 2: assign final values
            foreach (var item in request.Items)
            {
                byId[item.Id].Reorder(item.SortOrder);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("FaqItem.ConcurrencyConflict", "FAQ items were modified concurrently."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForFaq(request.EntityType, request.EntityId), ct);
            logger.LogInformation("Reordered {Count} FAQ items for {EntityType}/{EntityId}", request.Items.Count, request.EntityType, request.EntityId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
