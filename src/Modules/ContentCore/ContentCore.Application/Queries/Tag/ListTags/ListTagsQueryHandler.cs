using ContentCore.Application.Queries.Tag.Common;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Queries.Tag.ListTags;

public sealed class ListTagsQueryHandler(
    ITagRepository tagRepository,
    ILogger<ListTagsQueryHandler> logger)
    : IQueryHandler<ListTagsQuery, IReadOnlyList<TagDto>>
{
    public async Task<Result<IReadOnlyList<TagDto>>> Handle(
        ListTagsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tags = await tagRepository.GetAllAsync(
                filter: request.ActiveOnly ? t => t.IsActive : null,
                orderBy: q => q.OrderBy(t => t.Name),
                include: request.WithTranslations
                    ? q => q.Include(t => t.Translations)
                    : null,
                ct: cancellationToken);

            var dtos = tags
                .Select(t => new TagDto(
                    t.Id,
                    t.Name,
                    t.Slug,
                    t.IsActive,
                    request.WithTranslations
                        ? t.Translations
                            .Select(tr => new TagTranslationDto(tr.LanguageId, tr.Name, tr.Slug))
                            .ToList()
                        : null))
                .ToList() as IReadOnlyList<TagDto>;

            logger.LogDebug("ListTags returned {Count} tags (withTranslations={WithTranslations})",
                dtos.Count, request.WithTranslations);

            return Result<IReadOnlyList<TagDto>>.Success(dtos);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<IReadOnlyList<TagDto>>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
