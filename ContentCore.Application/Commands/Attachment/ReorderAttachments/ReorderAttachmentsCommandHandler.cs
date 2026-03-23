using ContentCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.ReorderAttachments;

public sealed class ReorderAttachmentsCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<ReorderAttachmentsCommand>
{
    public async Task<Result> Handle(ReorderAttachmentsCommand request, CancellationToken ct)
    {
        try
        {
            var attachments = await attachmentRepository.GetAllAsync(
                filter: x => x.EntityType == request.EntityType && x.EntityId == request.EntityId,
                asNoTracking: false,
                ct: ct);

            if (attachments.Count == 0)
                return Result.Failure(
                    new Error("Attachment.NotFound",
                        $"No attachments found for {request.EntityType}/{request.EntityId}."),
                    Outcome.NotFound);

            var attachmentMap = attachments.ToDictionary(a => a.Id);

            for (var i = 0; i < request.OrderedAttachmentIds.Count; i++)
            {
                if (!attachmentMap.TryGetValue(request.OrderedAttachmentIds[i], out var attachment))
                    return Result.Failure(
                        new Error("Attachment.NotFound",
                            $"Attachment '{request.OrderedAttachmentIds[i]}' does not belong to this entity."),
                        Outcome.NotFound);

                attachment.SetSortOrder(i);
            }

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure(
                    new Error("Attachment.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync($"attachments:{request.EntityType}:{request.EntityId}", ct);

            return Result.Success();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
