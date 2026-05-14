// <copyright file="SeoMetadataDto.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.SeoMetadata.Common;

using ContentSeo.Domain.Enums;

/// <summary>
/// DTO for SEO metadata.
/// </summary>
public sealed record SeoMetadataDto(
    Guid Id,
    SeoEntityType EntityType,
    Guid EntityId,
    string? MetaTitle,
    string? MetaDescription,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? SchemaMarkup,
    decimal SitemapPriority,
    string? SitemapChangeFrequency,
    string? CanonicalUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static SeoMetadataDto From(ContentSeo.Domain.Entities.SeoMetadata entity) =>
        new(
            entity.Id,
            entity.EntityType,
            entity.EntityId,
            entity.MetaTitle,
            entity.MetaDescription,
            entity.OgTitle,
            entity.OgDescription,
            entity.OgImageUrl,
            entity.SchemaMarkup,
            entity.SitemapPriority,
            entity.SitemapChangeFrequency,
            entity.CanonicalUrl,
            entity.CreatedAt,
            entity.UpdatedAt);
}
