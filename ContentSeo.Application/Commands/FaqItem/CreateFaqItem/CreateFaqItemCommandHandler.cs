// <copyright file="CreateFaqItemCommandHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.FaqItem.CreateFaqItem;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

using DomainFaqItem = ContentSeo.Domain.Entities.FaqItem;

public sealed class CreateFaqItemCommandHandler(
    IFaqItemRepository faqItemRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<CreateFaqItemCommandHandler> logger)
    : ICommandHandler<CreateFaqItemCommand, CreateFaqItemResult>
{
    public async Task<Result<CreateFaqItemResult>> Handle(CreateFaqItemCommand request, CancellationToken ct)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result<CreateFaqItemResult>.Failure(
                    Error.Unauthorized("Authentication is required to create FAQ items."),
                    Outcome.Unauthorized);
            }

            // Check sort-order uniqueness within entity
            var existing = await faqItemRepository.GetByEntityAsync(request.EntityType, request.EntityId, ct);
            if (existing.Any(e => e.SortOrder == request.SortOrder))
            {
                return Result<CreateFaqItemResult>.Failure(
                    new Error("FaqItem.SortOrderConflict", $"SortOrder {request.SortOrder} already used for this entity."),
                    Outcome.Conflict);
            }

            var entity = DomainFaqItem.Create(request.EntityType, request.EntityId, request.Question, request.Answer, request.SortOrder);
            await faqItemRepository.AddAsync(entity, ct);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateFaqItemResult>.Failure(
                    new Error("FaqItem.ConcurrencyConflict", "FAQ item was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForFaq(request.EntityType, request.EntityId), ct);

            logger.LogInformation("Created FAQ item {Id} for {EntityType}/{EntityId}", entity.Id, request.EntityType, request.EntityId);

            return Result<CreateFaqItemResult>.Created(new CreateFaqItemResult(entity.Id));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<CreateFaqItemResult>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
