// <copyright file="UpsertSeoMetadataCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.SeoMetadata.UpsertSeoMetadata;

using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record UpsertSeoMetadataCommand(
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
    string? CanonicalUrl) : ICommand<UpsertSeoMetadataResult>;

public sealed record UpsertSeoMetadataResult(Guid Id, bool Created);
