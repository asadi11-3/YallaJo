// <copyright file="UpsertSeoMetadataRequest.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Presentation.Endpoints.SeoMetadata.Models;

using ContentSeo.Domain.Enums;

public sealed record UpsertSeoMetadataRequest(
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
    string? CanonicalUrl);

public sealed record UpdateSeoMetadataRequest(
    string? MetaTitle,
    string? MetaDescription,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? SchemaMarkup,
    decimal SitemapPriority,
    string? SitemapChangeFrequency,
    string? CanonicalUrl);
