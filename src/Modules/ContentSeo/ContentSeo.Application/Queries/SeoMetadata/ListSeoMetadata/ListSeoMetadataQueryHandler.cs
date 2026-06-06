// <copyright file="ListSeoMetadataQueryHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.SeoMetadata.ListSeoMetadata;

using ContentSeo.Application.Queries.SeoMetadata.Common;
using ContentSeo.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class ListSeoMetadataQueryHandler(
    ISeoMetadataRepository seoMetadataRepository,
    ILogger<ListSeoMetadataQueryHandler> logger)
    : IQueryHandler<ListSeoMetadataQuery, ListSeoMetadataResult>
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    public async Task<Result<ListSeoMetadataResult>> Handle(ListSeoMetadataQuery request, CancellationToken ct)
    {
        try
        {
            var take = request.Take <= 0 ? DefaultTake : Math.Min(request.Take, MaxTake);
            var skip = request.Skip < 0 ? 0 : request.Skip;

            var (items, total) = await seoMetadataRepository.ListAsync(
                request.EntityType, request.IncludeDeleted, skip, take, ct);

            logger.LogDebug(
                "Listed {Count} SEO metadata records (total {Total}) entityType={EntityType} includeDeleted={IncludeDeleted}",
                items.Count, total, request.EntityType, request.IncludeDeleted);

            return Result<ListSeoMetadataResult>.Success(new ListSeoMetadataResult(
                items.Select(SeoMetadataDto.From).ToList(),
                total,
                skip,
                take));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<ListSeoMetadataResult>.Failure(
                new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
