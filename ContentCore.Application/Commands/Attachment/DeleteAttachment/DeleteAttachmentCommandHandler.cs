using ContentCore.Domain.Exceptions;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using Outcome = YallaJo.SharedKernel.Domain.Abstractions.Results.Outcome;

namespace ContentCore.Application.Commands.Attachment.DeleteAttachment;

public sealed class DeleteAttachmentCommandHandler(
    IAttachmentRepository attachmentRepository,
    IContentCoreUnitOfWork unitOfWork,
    HybridCache cache)
    : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var attachment = await attachmentRepository.GetByIdAsync(
                request.AttachmentId, cancellationToken, asNoTracking: false);

            if (attachment is null)
            {
                return Result.Failure(
                   new Error("Attachment.NotFound", $"Attachment '{request.AttachmentId}' was not found."),
                   Outcome.NotFound);
            }

            // Raises AttachmentDeletedDomainEvent → handler deletes the physical file
            attachment.MarkForDeletion();

            attachmentRepository.Remove(attachment);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ContentCoreConcurrencyException)
            {
                return Result.Failure(
                    new Error(
                        "Attachment.ConcurrencyConflict",
                        "This record was modified by another user. Please refresh and try again."),
                    Outcome.Conflict);
            }

            await cache.RemoveByTagAsync("attachments", cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(
                new Error("Request.Cancelled", "The request was cancelled."),
                Outcome.Canceled);
        }
    }
}
