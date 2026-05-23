using Booking.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Commands.UpdateProviderDocument;

internal sealed class UpdateProviderDocumentCommandHandler(
    IProviderDocumentRepository documentRepository,
    IBookingUnitOfWork unitOfWork,
    IAttachmentService attachmentService,
    ICurrentUser currentUser,
    HybridCache cache) : ICommandHandler<UpdateProviderDocumentCommand>
{
    public async Task<Result> Handle(UpdateProviderDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("Authentication required."), Outcome.Unauthorized);

        var doc = await documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (doc is null)
            return Result.Failure(new Error("ProviderDocument.NotFound", "Document not found."), Outcome.NotFound);

        if (doc.ProviderId != currentUser.UserId.Value)
            return Result.Failure(Error.Forbidden("You do not have permission to edit this document."), Outcome.Forbidden);

        Guid? newAttachmentId = null;
        string? newFileName = null;

        if (request.File is null == false)
        {
            if (request.File.Length > 10 * 1024 * 1024)
                return Result.Failure(new Error("ProviderDocument.FileTooLarge", "File size exceeds 10MB."), Outcome.Conflict);

            var allowedMimes = new[] { "application/pdf", "image/jpeg", "image/png" };
            if (!allowedMimes.Contains(request.File.ContentType))
                return Result.Failure(new Error("ProviderDocument.UnsupportedType", "Unsupported file type."), Outcome.Conflict);

            newAttachmentId = await attachmentService.UploadEncryptedAsync(request.File, "ProviderDocument", cancellationToken);
            newFileName = request.File.FileName;
        }

        var updateResult = doc.UpdateDetails(newAttachmentId, newFileName, request.ExpiresAt);
        if (updateResult.IsFailure)
            return updateResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"provider-documents:{doc.ProviderId}", cancellationToken);
        await cache.RemoveByTagAsync($"provider-document:{doc.Id}", cancellationToken);

        return Result.Success();
    }
}
