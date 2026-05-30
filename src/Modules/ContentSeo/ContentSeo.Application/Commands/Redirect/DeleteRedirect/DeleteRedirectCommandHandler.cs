// <copyright file="DeleteRedirectCommandHandler.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace ContentSeo.Application.Commands.Redirect.DeleteRedirect;

using ContentSeo.Application.Caching;
using ContentSeo.Application.Interfaces;
using ContentSeo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

public sealed class DeleteRedirectCommandHandler(
    IRedirectRepository redirectRepository,
    IContentSeoUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<DeleteRedirectCommandHandler> logger)
    : ICommandHandler<DeleteRedirectCommand>
{
    public async Task<Result> Handle(DeleteRedirectCommand request, CancellationToken ct)
    {
        try
        {
            // Auth handled by endpoint MustHavePermissionAttribute.

            var entity = await redirectRepository.GetByIdAsync(request.Id, ct);
            if (entity is null)
            {
                return Result.Failure(
                    new Error("Redirect.NotFound", $"Redirect {request.Id} not found."),
                    Outcome.NotFound);
            }

            entity.Deactivate();
            entity.SoftDelete();

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(new Error("Redirect.ConcurrencyConflict", "Redirect was modified concurrently."), Outcome.Conflict);
            }

            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagRedirectsList, ct);
            await cache.RemoveByTagAsync(ContentSeoCacheKeys.TagRedirectsLookup, ct);

            logger.LogInformation("Deleted redirect {Id} ({OldUrl})", entity.Id, entity.OldUrl);
            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(new Error("Request.Cancelled", "Operation was cancelled."), Outcome.Canceled);
        }
    }
}
