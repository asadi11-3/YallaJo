// <copyright file="ListRedirectsQueryHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.Redirect.ListRedirects;

using ContentSeo.Application.Interfaces;
using ContentSeo.Application.Queries.Redirect.Common;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class ListRedirectsQueryHandler(
    IRedirectRepository redirectRepository,
    ILogger<ListRedirectsQueryHandler> logger)
    : IQueryHandler<ListRedirectsQuery, PaginatedResult<RedirectDto>>
{
    public async Task<Result<PaginatedResult<RedirectDto>>> Handle(ListRedirectsQuery request, CancellationToken ct)
    {
        try
        {
            // Bound page size
            var pageSize = Math.Clamp(request.PageSize, 1, 200);
            var page = Math.Max(1, request.Page);

            var all = await redirectRepository.GetAllAsync(
                filter: r => !r.IsDeleted
                    && (request.IsActive == null || r.IsActive == request.IsActive)
                    && (request.StatusCode == null || r.StatusCode == request.StatusCode)
                    && (request.OldUrl == null || r.OldUrl.Contains(request.OldUrl)),
                orderBy: q => q.OrderByDescending(r => r.CreatedAt),
                asNoTracking: true,
                ct: ct);

            var total = all.Count;
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).Select(RedirectDto.From).ToList();

            logger.LogDebug("Listed {Count} redirects (total={Total}, page={Page})", items.Count, total, page);

            return Result<PaginatedResult<RedirectDto>>.Success(new PaginatedResult<RedirectDto>(items, total, page, pageSize));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<PaginatedResult<RedirectDto>>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
