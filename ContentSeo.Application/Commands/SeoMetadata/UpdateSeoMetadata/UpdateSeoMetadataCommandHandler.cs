// <copyright file="UpdateSeoMetadataCommandHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.SeoMetadata.UpdateSeoMetadata;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class UpdateSeoMetadataCommandHandler(
    ISeoMetadataRepository seoMetadataRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<UpdateSeoMetadataCommandHandler> logger)
    : ICommandHandler<UpdateSeoMetadataCommand>
{
    public async Task<Result> Handle(UpdateSeoMetadataCommand request, CancellationToken ct)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result.Failure(
                    Error.Unauthorized("Authentication is required to update SEO metadata."),
                    Outcome.Unauthorized);
            }

            var entity = await seoMetadataRepository.GetByIdAsync(request.Id, ct);
            if (entity is null)
            {
                return Result.Failure(
                    new Error("SeoMetadata.NotFound", $"SEO metadata {request.Id} not found."),
                    Outcome.NotFound);
            }

            entity.UpdateMeta(request.MetaTitle, request.MetaDescription);
            entity.UpdateOg(request.OgTitle, request.OgDescription, request.OgImageUrl);
            entity.UpdateSchema(request.SchemaMarkup);
            entity.UpdateSitemapHints(request.SitemapPriority, request.SitemapChangeFrequency, request.CanonicalUrl);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("SeoMetadata.ConcurrencyConflict", "SEO metadata was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForSeo(entity.EntityType, entity.EntityId), ct);
            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForSitemap(entity.EntityType), ct);

            logger.LogInformation("Updated SEO metadata {Id} for {EntityType}/{EntityId}", entity.Id, entity.EntityType, entity.EntityId);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
