using System.Threading;
using System.Threading.Tasks;
using Booking.Application.Interfaces;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetProviderDocument;

internal sealed class GetProviderDocumentQueryHandler(
    IProviderDocumentRepository documentRepository,
    ICurrentUser currentUser) : IQueryHandler<GetProviderDocumentQuery, ProviderDocumentDto>
{
    public async Task<Result<ProviderDocumentDto>> Handle(GetProviderDocumentQuery request, CancellationToken cancellationToken)
    {
        var doc = await documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);

        if (doc is null)
        {
            return Result<ProviderDocumentDto>.Failure(
                new Error("ProviderDocument.NotFound", "Document not found."), Outcome.NotFound);
        }

        if (doc.ProviderId != currentUser.UserId)
        {
            return Result<ProviderDocumentDto>.Failure(
                Error.Forbidden("You do not have permission to view this document."), Outcome.Forbidden);
        }

        var dto = new ProviderDocumentDto(
            doc.Id,
            doc.ProviderId,
            doc.DocumentType,
            doc.AttachmentId,
            doc.OriginalFileName,
            doc.ExpiresAt,
            doc.Status,
            doc.ExpiryWarningSent,
            doc.ExpiryProcessed
        );

        return Result<ProviderDocumentDto>.Success(dto);
    }
}
