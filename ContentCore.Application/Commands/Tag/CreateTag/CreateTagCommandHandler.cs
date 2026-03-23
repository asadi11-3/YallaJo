using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Tag.CreateTag;

public sealed class CreateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<CreateTagCommand, CreateTagResult>
{
    public async Task<Result<CreateTagResult>> Handle(
        CreateTagCommand request,
        CancellationToken ct)
    {
        try
        {
            if (await tagRepository.SlugExistsAsync(request.Slug, ct))
                return Result<CreateTagResult>.Conflict(
                    new Error("Tag.AlreadyExists", $"Tag with slug '{request.Slug}' already exists."));

            var tag = Domain.Entities.Tag.Create(request.Name, request.Slug);

            await tagRepository.AddAsync(tag, ct);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<CreateTagResult>.Conflict(
                    new Error("Tag.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("tags", ct);

            return Result<CreateTagResult>.Created(
                new CreateTagResult(tag.Id, tag.Name, tag.Slug));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result<CreateTagResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
