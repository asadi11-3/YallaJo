// <copyright file="UpdateSeoMetadataCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.SeoMetadata.UpdateSeoMetadata;

using YallaJo.SharedKernel.Application.Abstractions.Messaging;

public sealed record UpdateSeoMetadataCommand(
    Guid Id,
    string? MetaTitle,
    string? MetaDescription,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? SchemaMarkup,
    decimal SitemapPriority,
    string? SitemapChangeFrequency,
    string? CanonicalUrl) : ICommand;
