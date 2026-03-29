using ContentCore.Application.Caching;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Tag.UpdateTag;

public sealed class UpdateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<UpdateTagCommand, UpdateTagResult>
{
    public async Task<Result<UpdateTagResult>> Handle(
        UpdateTagCommand request,
        CancellationToken ct)
    {
        try
        {
            var tag = await tagRepository.GetByIdAsync(request.Id, ct, asNoTracking: false);
            if (tag is null) {
                return Result<UpdateTagResult>.Failure(
                       new Error("Tag.NotFound", $"Tag '{request.Id}' was not found."),
                       Outcome.NotFound);
            }


            if (await tagRepository.SlugExistsAsync(request.Slug, request.Id, ct)) {
                return Result<UpdateTagResult>.Conflict(
                       new Error("Tag.AlreadyExists", $"Tag with slug '{request.Slug}' already exists."));
            }
            tag.Update(request.Name, request.Slug);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result<UpdateTagResult>.Conflict(
                    new Error(
                        "Tag.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("tags", ct);

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
