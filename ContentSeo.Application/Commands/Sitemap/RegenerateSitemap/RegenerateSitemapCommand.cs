// <copyright file="RegenerateSitemapCommand.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Sitemap.RegenerateSitemap;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed record RegenerateSitemapCommand : ICommand<RegenerateSitemapResult>;

public sealed record RegenerateSitemapResult(int UrlCount, DateTime RenderedAt);

public sealed class RegenerateSitemapCommandHandler(
    ISitemapRenderer renderer,
    HybridCache cache,
    ICurrentUser currentUser,
    ILogger<RegenerateSitemapCommandHandler> logger)
    : ICommandHandler<RegenerateSitemapCommand, RegenerateSitemapResult>
{
    public async Task<Result<RegenerateSitemapResult>> Handle(RegenerateSitemapCommand request, CancellationToken ct)
    {
        try
        {
            if (currentUser.UserId is null)
            {
                return Result<RegenerateSitemapResult>.Failure(Error.Unauthorized("Authentication is required."), Outcome.Unauthorized);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagSitemapRendered, ct);
            var xml = await renderer.RenderAndCacheAsync(ct);

            var urlCount = CountOccurrences(xml, "<url>");
            logger.LogInformation("Regenerated sitemap with {Count} URLs", urlCount);

            return Result<RegenerateSitemapResult>.Success(new RegenerateSitemapResult(urlCount, DateTime.UtcNow));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Sitemap.SizeOverflow", StringComparison.Ordinal))
        {
            return Result<RegenerateSitemapResult>.Failure(
                new Error("Sitemap.SizeOverflow", "Sitemap exceeds 50,000 entry limit."),
                Outcome.ServerError);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<RegenerateSitemapResult>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }

    private static int CountOccurrences(string text, string substring)
    {
        if (string.IsNullOrEmpty(substring))
        {
            return 0;
        }

        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(substring, idx, StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += substring.Length;
        }

        return count;
    }
}
