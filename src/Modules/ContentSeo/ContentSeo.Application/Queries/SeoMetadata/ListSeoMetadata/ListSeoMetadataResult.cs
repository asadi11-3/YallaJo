// <copyright file="ListSeoMetadataResult.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Queries.SeoMetadata.ListSeoMetadata;

using ContentSeo.Application.Queries.SeoMetadata.Common;

public sealed record ListSeoMetadataResult(
    IReadOnlyList<SeoMetadataDto> Items,
    int Total,
    int Skip,
    int Take);
