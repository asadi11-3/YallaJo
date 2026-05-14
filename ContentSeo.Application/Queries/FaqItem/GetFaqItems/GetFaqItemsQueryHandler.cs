// <copyright file="GetFaqItemsQueryHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.FaqItem.GetFaqItems;

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using ContentSeo.Application.Queries.Common;
using ContentSeo.Application.Queries.FaqItem.Common;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class GetFaqItemsQueryHandler(
    IFaqItemRepository faqItemRepository,
    IActiveLanguageProvider languageProvider,
    ILogger<GetFaqItemsQueryHandler> logger)
    : IQueryHandler<GetFaqItemsQuery, IReadOnlyList<FaqItemDto>>
{
    public async Task<Result<IReadOnlyList<FaqItemDto>>> Handle(GetFaqItemsQuery request, CancellationToken ct)
    {
        try
        {
            var items = await faqItemRepository.GetByEntityWithTranslationsAsync(request.EntityType, request.EntityId, ct);

            // AcceptLanguageResolver returns null when no translation match exists; consumer (FaqItemDto.From) falls back to source language in that case.
            var languageId = await AcceptLanguageResolver.ResolveAsync(request.AcceptLanguage, languageProvider, ct);

            var active = items.Where(i => i.IsActive).Select(i => FaqItemDto.From(i, languageId)).ToList();

            logger.LogDebug("Returned {Count} FAQ items for {EntityType}/{EntityId}", active.Count, request.EntityType, request.EntityId);

            return Result<IReadOnlyList<FaqItemDto>>.Success(active);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<IReadOnlyList<FaqItemDto>>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
