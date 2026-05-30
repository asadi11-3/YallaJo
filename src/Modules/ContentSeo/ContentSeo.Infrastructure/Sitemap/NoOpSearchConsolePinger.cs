// <copyright file="NoOpSearchConsolePinger.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Infrastructure.Sitemap;

using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.Extensions.Logging;

internal sealed class NoOpSearchConsolePinger(ILogger<NoOpSearchConsolePinger> logger) : ISearchConsolePinger
{
    public Task PingAsync(string sitemapUrl, CancellationToken ct)
    {
        logger.LogInformation("NoOpSearchConsolePinger: would have notified search engines about {SitemapUrl}", sitemapUrl);
        return Task.CompletedTask;
    }
}
