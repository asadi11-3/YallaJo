using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Attachment.ReorderAttachments;

public sealed class ReorderAttachmentsCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<ReorderAttachmentsCommand>
{
    public async Task<Result> Handle(ReorderAttachmentsCommand request, CancellationToken ct)
    {
        var attachments = await attachmentRepository.GetAllAsync(
            filter: x => x.EntityType == request.EntityType && x.EntityId == request.EntityId,
            asNoTracking: false,
            ct: ct);

        if (attachments.Count == 0)
            return Result.NotFound($"No attachments found for {request.EntityType}/{request.EntityId}.");

        var attachmentMap = attachments.ToDictionary(a => a.Id);

        for (var i = 0; i < request.OrderedAttachmentIds.Count; i++)
        {
            if (!attachmentMap.TryGetValue(request.OrderedAttachmentIds[i], out var attachment))
                return Result.Invalid(new Error("InvalidId",
                    $"Attachment '{request.OrderedAttachmentIds[i]}' does not belong to this entity."));

            attachment.SetSortOrder(i);
        }

        await unitOfWork.SaveChangesAsync(ct);
        cache.Remove(ContentCoreCacheKeys.EntityAttachments(request.EntityType.ToString(), request.EntityId));

        return Result.Success();
    }
}