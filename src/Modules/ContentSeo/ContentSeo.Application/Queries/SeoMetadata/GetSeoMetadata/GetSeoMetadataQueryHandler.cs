// <copyright file="GetSeoMetadataQueryHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.SeoMetadata.GetSeoMetadata;

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Application.Queries.SeoMetadata.Common;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class GetSeoMetadataQueryHandler(
    ISeoMetadataRepository seoMetadataRepository,
    ILogger<GetSeoMetadataQueryHandler> logger)
    : IQueryHandler<GetSeoMetadataQuery, SeoMetadataDto>
{
    public async Task<Result<SeoMetadataDto>> Handle(GetSeoMetadataQuery request, CancellationToken ct)
    {
        try
        {
            var entity = await seoMetadataRepository.GetByEntityAsync(request.EntityType, request.EntityId, ct);
            if (entity is null)
            {
                return Result<SeoMetadataDto>.Failure(
                    new Error("SeoMetadata.NotFound", $"No SEO metadata found for {request.EntityType}/{request.EntityId}."),
                    Outcome.NotFound);
            }

            logger.LogDebug("Resolved SEO metadata for {EntityType}/{EntityId}", request.EntityType, request.EntityId);
            return Result<SeoMetadataDto>.Success(SeoMetadataDto.From(entity));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<SeoMetadataDto>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
