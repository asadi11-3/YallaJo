// <copyright file="CreateRedirectCommandHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Redirect.CreateRedirect;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

using DomainRedirect = ContentSeo.Domain.Entities.Redirect;

public sealed class CreateRedirectCommandHandler(
    IRedirectRepository redirectRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<CreateRedirectCommandHandler> logger)
    : ICommandHandler<CreateRedirectCommand, CreateRedirectResult>
{
    private const int MaxHops = 3;

    public async Task<Result<CreateRedirectResult>> Handle(CreateRedirectCommand request, CancellationToken ct)
    {
        try
        {
            // Auth handled by endpoint MustHavePermissionAttribute.

            var oldUrl = request.OldUrl.Trim();
            var newUrl = request.NewUrl.Trim();

            // Duplicate check
            var duplicate = await redirectRepository.GetActiveByOldUrlAsync(oldUrl, ct);
            if (duplicate is not null)
            {
                return Result<CreateRedirectResult>.Failure(
                    new Error("Redirect.DuplicateOldUrl", $"An active redirect already exists for {oldUrl}."),
                    Outcome.Conflict);
            }

            // Walk forward to detect cycles and compute final target
            var currentTarget = newUrl;
            var hops = 0;
            while (true)
            {
                var next = await redirectRepository.GetActiveByOldUrlAsync(currentTarget, ct);
                if (next is null)
                {
                    break;
                }

                currentTarget = next.NewUrl;
                hops++;

                if (string.Equals(currentTarget, oldUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return Result<CreateRedirectResult>.Failure(
                        new Error("Redirect.CircularChain", "Creating this redirect would form a cycle."),
                        Outcome.Conflict);
                }

                if (hops > MaxHops)
                {
                    return Result<CreateRedirectResult>.Failure(
                        new Error("Redirect.CircularChain", $"Redirect chain exceeds {MaxHops} hops."),
                        Outcome.Conflict);
                }
            }

            var finalTarget = currentTarget;

            // Flatten existing chains pointing at oldUrl
            var existing = await redirectRepository.GetActivePointingToAsync(oldUrl, ct);
            foreach (var r in existing)
            {
                r.RewriteTo(finalTarget, hopsCollapsed: 1);
            }

            // Create the new redirect
            var entity = DomainRedirect.Create(oldUrl, finalTarget, request.StatusCode);
            await redirectRepository.AddAsync(entity, ct);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateRedirectResult>.Failure(
                    new Error("Redirect.ConcurrencyConflict", "Redirect was modified concurrently."),
                    Outcome.Conflict);
            }
            catch (DbUpdateException ex)
            {
                if (await IsDuplicateAsync(oldUrl, entity.Id, ct))
                {
                    return Result<CreateRedirectResult>.Failure(
                        new Error("Redirect.DuplicateOldUrl", $"An active redirect already exists for {oldUrl}."),
                        Outcome.Conflict);
                }

                logger.LogError(ex, "Unexpected database error creating redirect {OldUrl}", Sanitize(oldUrl));
                return Result<CreateRedirectResult>.Failure(
                    new Error("Redirect.DatabaseError", "Unexpected database error creating redirect."),
                    Outcome.ServerError);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagRedirectsList, ct);
            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagRedirectsLookup, ct);

            logger.LogInformation("Created redirect {Id}: {OldUrl} -> {Target} (flattened {Existing} existing chains)", entity.Id, Sanitize(oldUrl), Sanitize(finalTarget), existing.Count);

            return Result<CreateRedirectResult>.Created(new CreateRedirectResult(entity.Id, finalTarget, existing.Count));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<CreateRedirectResult>.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }

    private async Task<bool> IsDuplicateAsync(string oldUrl, Guid excludeId, CancellationToken ct)
    {
        var dup = await redirectRepository.GetActiveByOldUrlAsync(oldUrl, ct);
        return dup is not null && dup.Id != excludeId;
    }

    private static string Sanitize(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        var qIdx = url.IndexOf('?');
        return qIdx >= 0 ? url[..qIdx] : url;
    }
}
