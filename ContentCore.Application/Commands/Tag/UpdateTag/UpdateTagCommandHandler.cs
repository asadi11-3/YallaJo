using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Tag.UpdateTag;

public sealed class UpdateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<UpdateTagCommandHandler> logger)
    : ICommandHandler<UpdateTagCommand, UpdateTagResult>
{
    public async Task<Result<UpdateTagResult>> Handle(
        UpdateTagCommand request,
        CancellationToken ct)
    {
        try
        {
            var tag = await tagRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (tag is null)
            {
                return Result<UpdateTagResult>.Failure(
                       new Error("Tag.NotFound", $"Tag '{request.Id}' was not found."),
                       Outcome.NotFound);
            }

            if (await tagRepository.AnyAsync(t => t.Slug == request.Slug && t.Id != request.Id, ct))
            {
                return Result<UpdateTagResult>.Conflict(
                       new Error("Tag.AlreadyExists", $"Tag with slug '{request.Slug}' already exists."));
            }

            tag.Update(request.Name, request.Slug, request.SourceLanguageCode);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<UpdateTagResult>.Conflict(
                    new Error(
                        "Tag.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("tags", ct);

            logger.LogInformation(
                "Tag updated: {TagId} (Name={Name}, Slug={Slug})", tag.Id, tag.Name, tag.Slug);

            return Result<UpdateTagResult>.Success(
                new UpdateTagResult(tag.Id, tag.Name, tag.Slug));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<UpdateTagResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
