using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Application.Queries.Attachment.GetEntityAttachments;

public sealed class GetEntityAttachmentsQueryHandler(
    IAttachmentRepository attachmentRepository)
    : IQueryHandler<GetEntityAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    public async Task<Result<IReadOnlyList<AttachmentDto>>> Handle(
        GetEntityAttachmentsQuery request,
        CancellationToken ct)
    {
        var attachments = await attachmentRepository.GetAllAsync(
            filter: x => x.EntityType == request.EntityType && x.EntityId == request.EntityId,
            orderBy: q => q.OrderBy(x => x.SortOrder),
            ct: ct);

        var dtos = attachments.Select(a => new AttachmentDto(
            a.Id,
            a.EntityType,
            a.EntityId,
            a.Type,
            a.Url,
            a.ThumbnailUrl,
            a.OriginalFileName,
            a.MimeType,
            a.FileSize,
            a.Width,
            a.Height,
            a.DurationSeconds,
            a.SortOrder,
            a.UploadedAt,
            a.UploadedByUserId)).ToList();

        return Result<IReadOnlyList<AttachmentDto>>.Success(dtos);
    }
}