// <copyright file="GetAllFaqItemsQueryHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.FaqItem.GetAllFaqItems;

using ContentSeo.Application.Queries.Common;
using ContentSeo.Application.Queries.FaqItem.Common;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

/// <summary>
/// Reference pattern: <see cref="Redirect.ListRedirects.ListRedirectsQueryHandler"/>
/// for pagination + clamping, and ContentCore.Application Queries.Tag.ListTags
/// for the include-translations pipeline. Closes finding F12.3.
/// </summary>
public sealed class GetAllFaqItemsQueryHandler(
    IFaqItemRepository faqItemRepository,
    IActiveLanguageProvider languageProvider,
    ILogger<GetAllFaqItemsQueryHandler> logger)
    : IQueryHandler<GetAllFaqItemsQuery, PaginatedResult<FaqItemDto>>
{
    public async Task<Result<PaginatedResult<FaqItemDto>>> Handle(GetAllFaqItemsQuery request, CancellationToken ct)
    {
        try
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 200);

            // Translations are eagerly included so FaqItemDto.From can pick the Accept-Language match
            // (falls back to source language when no match exists).
            var all = await faqItemRepository.GetAllAsync(
                filter: f => (!request.ActiveOnly || f.IsActive)
                          && (request.EntityType == null || f.EntityType == request.EntityType),
                orderBy: q => q.OrderBy(f => f.EntityType).ThenBy(f => f.SortOrder),
                include: q => q.Include(f => f.FaqItemTranslations),
                asNoTracking: true,
                ct: ct);

            var total = all.Count;

            var languageId = await AcceptLanguageResolver.ResolveAsync(request.AcceptLanguage, languageProvider, ct);

            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => FaqItemDto.From(i, languageId))
                .ToList();

            logger.LogDebug(
                "GetAllFaqItems returned {Count} of {Total} (page={Page} size={PageSize} entityType={EntityType} activeOnly={ActiveOnly})",
                items.Count, total, page, pageSize, request.EntityType, request.ActiveOnly);

            return Result<PaginatedResult<FaqItemDto>>.Success(
                new PaginatedResult<FaqItemDto>(items, total, page, pageSize));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<PaginatedResult<FaqItemDto>>.Failure(
                new Error("Request.Cancelled", "Operation was cancelled."),
                Outcome.Canceled);
        }
    }
}
