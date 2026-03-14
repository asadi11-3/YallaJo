using ContentCore.Application.Caching;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Commands.Attachment.DeleteAttachment;

public sealed class DeleteAttachmentCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    IMemoryCache cache)
    : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken ct)
    {
        var attachment = await attachmentRepository.GetByIdAsync(
            request.AttachmentId, ct, asNoTracking: false);

        if (attachment is null)
            return Result.NotFound($"Attachment '{request.AttachmentId}' not found.");

        // Raises AttachmentDeletedDomainEvent → handler deletes the physical file
        attachment.MarkForDeletion();

        attachmentRepository.Remove(attachment);
        await unitOfWork.SaveChangesAsync(ct);
        cache.Remove(ContentCoreCacheKeys.Attachment(request.AttachmentId));

        return Result.Success();
    }
}