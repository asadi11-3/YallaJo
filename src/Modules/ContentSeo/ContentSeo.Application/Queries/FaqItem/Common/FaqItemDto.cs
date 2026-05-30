// <copyright file="FaqItemDto.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.FaqItem.Common;

using ContentSeo.Domain.Enums;
using DomainFaqItem = ContentSeo.Domain.Entities.FaqItem;

public sealed record FaqItemDto(
    Guid Id,
    SeoEntityType EntityType,
    Guid EntityId,
    string Question,
    string Answer,
    int SortOrder,
    bool IsActive)
{
    public static FaqItemDto From(DomainFaqItem entity, Guid? preferredLanguageId)
    {
        // Pull translation if exists; otherwise fall back to source.
        var translation = preferredLanguageId.HasValue
            ? entity.FaqItemTranslations.FirstOrDefault(t => t.LanguageId == preferredLanguageId.Value)
            : null;

        var question = translation?.Question ?? entity.Question;
        var answer = translation?.Answer ?? entity.Answer;

        return new FaqItemDto(
            entity.Id,
            entity.EntityType,
            entity.EntityId,
            question,
            answer,
            entity.SortOrder,
            entity.IsActive);
    }
}
