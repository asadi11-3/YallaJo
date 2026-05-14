// <copyright file="UpsertSeoMetadataCommandHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.SeoMetadata.UpsertSeoMetadata;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

using DomainSeoMetadata = ContentSeo.Domain.Entities.SeoMetadata;

public sealed class UpsertSeoMetadataCommandHandler(
    ISeoMetadataRepository seoMetadataRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpsertSeoMetadataCommandHandler> logger)
    : ICommandHandler<UpsertSeoMetadataCommand, UpsertSeoMetadataResult>
{
    public async Task<Result<UpsertSeoMetadataResult>> Handle(UpsertSeoMetadataCommand request, CancellationToken ct)
    {
        try
        {
            // Auth handled by endpoint MustHavePermissionAttribute.

            var existing = await seoMetadataRepository.GetByEntityAsync(request.EntityType, request.EntityId, ct);
            bool created;
            Guid id;

            if (existing is null)
            {
                var entity = DomainSeoMetadata.Create(
                    request.EntityType,
                    request.EntityId,
                    request.MetaTitle,
                    request.MetaDescription,
                    request.CanonicalUrl,
                    request.SitemapPriority,
                    request.SitemapChangeFrequency);

                entity.UpdateOg(request.OgTitle, request.OgDescription, request.OgImageUrl);
                entity.UpdateSchema(request.SchemaMarkup);
                entity.UpdateSitemapHints(request.SitemapPriority, request.SitemapChangeFrequency, request.CanonicalUrl);

                await seoMetadataRepository.AddAsync(entity, ct);
                id = entity.Id;
                created = true;
            }
            else
            {
                existing.UpdateMeta(request.MetaTitle, request.MetaDescription);
                existing.UpdateOg(request.OgTitle, request.OgDescription, request.OgImageUrl);
                existing.UpdateSchema(request.SchemaMarkup);
                existing.UpdateSitemapHints(request.SitemapPriority, request.SitemapChangeFrequency, request.CanonicalUrl);

                id = existing.Id;
                created = false;
            }

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpsertSeoMetadataResult>.Failure(
                    new Error("SeoMetadata.ConcurrencyConflict", "SEO metadata was modified concurrently."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForSeo(request.EntityType, request.EntityId), ct);
            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagForSitemap(request.EntityType), ct);

            logger.LogInformation("Upserted SEO metadata {Id} for {EntityType}/{EntityId} (created={Created})", id, request.EntityType, request.EntityId, created);

            var result = new UpsertSeoMetadataResult(id, created);
            return created
                ? Result<UpsertSeoMetadataResult>.Created(result)
                : Result<UpsertSeoMetadataResult>.Success(result);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<UpsertSeoMetadataResult>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
