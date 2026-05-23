using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Booking.Application.Interfaces;
using Booking.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Booking.Application.Queries.GetProviderDocuments;

internal sealed class GetProviderDocumentsQueryHandler(
    IProviderDocumentRepository documentRepository,
    ICurrentUser currentUser) : IQueryHandler<GetProviderDocumentsQuery, IReadOnlyList<ProviderDocumentDto>>
{
    public async Task<Result<IReadOnlyList<ProviderDocumentDto>>> Handle(GetProviderDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId != request.ProviderId)
        {
            return Result<IReadOnlyList<ProviderDocumentDto>>.Failure(
                Error.Forbidden("You can only view your own documents."), Outcome.Forbidden);
        }

        var docs = await documentRepository.GetAllByProviderIdAsync(request.ProviderId, cancellationToken);

        var dtos = docs.Select(d => new ProviderDocumentDto(
            d.Id,
            d.ProviderId,
            d.DocumentType,
            d.AttachmentId,
            d.OriginalFileName,
            d.ExpiresAt,
            d.Status,
            d.ExpiryWarningSent,
            d.ExpiryProcessed
        )).ToList();

        return Result<IReadOnlyList<ProviderDocumentDto>>.Success(dtos.AsReadOnly());
    }
}
