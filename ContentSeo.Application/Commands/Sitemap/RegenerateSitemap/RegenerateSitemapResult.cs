// <copyright file="RegenerateSitemapResult.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Sitemap.RegenerateSitemap;

public sealed record RegenerateSitemapResult(int UrlCount, DateTime RenderedAt);
