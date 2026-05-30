// <copyright file="UpdateFaqItemCommandHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.UpdateFaqItem;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class UpdateFaqItemCommandHandler(
    IFaqItemRepository faqItemRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateFaqItemCommandHandler> logger)
    : ICommandHandler<UpdateFaqItemCommand>
{
    public async Task<Result> Handle(UpdateFaqItemCommand request, CancellationToken ct)
    {
        try
        {
            // Auth handled by endpoint MustHavePermissionAttribute.

            var entity = await faqItemRepository.GetByIdAsync(request.Id, ct);
            if (entity is null)
            {
                return Result.Failure(new Error("FaqItem.NotFound", $"FAQ item {request.Id} not found."), Outcome.NotFound);
            }

            entity.Update(request.Question, request.Answer);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("FaqItem.ConcurrencyConflict", "FAQ item was modified concurrently."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForFaq(entity.EntityType, entity.EntityId), ct);
            logger.LogInformation("Updated FAQ item {Id}", entity.Id);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
