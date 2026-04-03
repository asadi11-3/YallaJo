
using ContentCore.Application.Caching;
using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;
using TagEntity = ContentCore.Domain.Entities.Tag;
namespace ContentCore.Application.Commands.Tag.CreateTag;

public sealed class CreateTagCommandHandler(
    ITagRepository tagRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<CreateTagCommand, CreateTagResult>
{
    public async Task<Result<CreateTagResult>> Handle(
        CreateTagCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await tagRepository.AnyAsync(t => t.Slug == request.Slug, cancellationToken))
            {
                return Result<CreateTagResult>.Conflict(
                   new Error("Tag.AlreadyExists", $"Tag with slug '{request.Slug}' already exists."));
            }

            var tag = TagEntity.Create(request.Name, request.Slug);

            await tagRepository.AddAsync(tag, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result<CreateTagResult>.Conflict(
                    new Error(
                        "Tag.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."));
            }

            await cache.RemoveByTagAsync("tags", cancellationToken);

            return Result<CreateTagResult>.Created(
                new CreateTagResult(tag.Id, tag.Name, tag.Slug));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<CreateTagResult>.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
