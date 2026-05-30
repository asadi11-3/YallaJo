// <copyright file="ISearchConsolePinger.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Interfaces;

/// <summary>
/// Notifies search engines (Google Search Console / Bing Webmaster Tools) about sitemap updates.
/// </summary>
public interface ISearchConsolePinger
{
    Task PingAsync(string sitemapUrl, CancellationToken ct);
}
