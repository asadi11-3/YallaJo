using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
namespace Booking.Application.Commands.UploadProviderDocument;

internal sealed class UploadProviderDocumentCommandHandler(
    IProviderDocumentRepository documentRepository,
    IBookingUnitOfWork unitOfWork,
    IAttachmentService attachmentService,
    ICurrentUser currentUser,
    HybridCache cache) : ICommandHandler<UploadProviderDocumentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UploadProviderDocumentCommand request, CancellationToken cancellationToken)
    {
        // 0. التأكد من هوية المزود
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<Guid>.Failure(Error.Unauthorized("Authentication required."), Outcome.Unauthorized);

        var providerId = currentUser.UserId.Value;

        // 1. التحقق من حجم الملف (Max 10MB)
        if (request.File.Length > 10 * 1024 * 1024)
            return Result<Guid>.Failure(
                new Error("ProviderDocument.FileTooLarge", "File size must not exceed 10MB."),
                Outcome.ServerError);

        // 2. التحقق من نوع الملف (MIME Type)
        var allowedMimes = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowedMimes.Contains(request.File.ContentType))
            return Result<Guid>.Failure(
                new Error("ProviderDocument.UnsupportedType", "Unsupported file type. Only PDF, JPEG, and PNG are allowed."),
                Outcome.ServerError);

        // 3. رفع الملف لخدمة المرفقات المُشفرة
        var attachmentId = await attachmentService.UploadEncryptedAsync(request.File, "ProviderDocument", cancellationToken);

        // 4. إنشاء كيان الوثيقة
        var doc = ProviderDocument.CreateForProvider(
            providerId,
            request.DocumentType,
            attachmentId,
            request.File.FileName,
            request.ExpiresAt
        );

        // 5. الحفظ في قاعدة البيانات
        documentRepository.Add(doc);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. مسح الـ Cache الخاص بالمزود
        await cache.RemoveByTagAsync($"provider-documents:{providerId}", cancellationToken);

        // 7. إرجاع الـ ID الجديد
        return Result<Guid>.Success(doc.Id);
    }
}
