// <copyright file="ListSeoMetadataQuery.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.SeoMetadata.ListSeoMetadata;

using ContentSeo.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

/// <summary>
/// Admin-side paginated list of SEO metadata records.
/// Not cached: admin tooling expects fresh reads and pagination filters change often.
/// </summary>
public sealed record ListSeoMetadataQuery(
    SeoEntityType? EntityType = null,
    bool IncludeDeleted = false,
    int Skip = 0,
    int Take = 50) : IQuery<ListSeoMetadataResult>;
