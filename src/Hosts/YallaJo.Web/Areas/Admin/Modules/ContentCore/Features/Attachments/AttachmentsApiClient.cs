using YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Attachments.Responses;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Attachments;

public sealed class AttachmentsApiClient
{
    private readonly IApiClient _api;
    public AttachmentsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<AttachmentItemResponse>>> GetEntityAttachmentsAsync(
        string entityType, Guid entityId, CancellationToken ct = default)
        => _api.GetAsync<List<AttachmentItemResponse>>(
            $"/api/v1/content-core/attachments?entityType={Uri.EscapeDataString(entityType)}&entityId={entityId}", ct);

    public Task<ApiResult<AttachmentItemResponse>> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        IReadOnlyDictionary<string, string> fields,
        CancellationToken ct = default)
        => _api.PostFileAsync<AttachmentItemResponse>(
            "/api/v1/content-core/attachments",
            stream, fileName, contentType,
            formFields: fields,
            formFieldName: "file",
            ct: ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/content-core/attachments/{id}", ct);

    public Task<ApiResult> SetPrimaryAsync(
        string entityType, Guid entityId, Guid attachmentId, CancellationToken ct = default)
        => _api.PutAsync("/api/v1/content-core/attachments/primary", new
        {
            EntityType   = entityType,
            EntityId     = entityId,
            AttachmentId = attachmentId,
        }, ct);
}
