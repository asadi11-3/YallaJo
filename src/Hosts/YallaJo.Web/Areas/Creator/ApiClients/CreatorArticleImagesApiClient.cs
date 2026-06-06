using YallaJo.Web.Areas.Creator.Models.Articles.Images;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Creator.ApiClients;

public sealed class CreatorArticleImagesApiClient
{
    private const string Base = "/api/v1/content-core/attachments";
    private const string BlogEntityType = "Blog";
    private const string ImageAttachmentType = "Image";

    private readonly IApiClient _api;

    public CreatorArticleImagesApiClient(IApiClient api) => _api = api;

    /// <summary>GET /attachments?entityType=Blog&amp;entityId={blogId} (Attachment.Read).</summary>
    public Task<ApiResult<List<AttachmentItemResponse>>> ListAsync(Guid blogId, CancellationToken ct = default)
        => _api.GetAsync<List<AttachmentItemResponse>>(
            $"{Base}?entityType={BlogEntityType}&entityId={blogId}", ct);

    public Task<ApiResult<UploadAttachmentResponse>> UploadAsync(
        Guid blogId, Stream fileStream, string fileName, string contentType, int sortOrder,
        CancellationToken ct = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["EntityType"]     = BlogEntityType,
            ["EntityId"]       = blogId.ToString(),
            ["AttachmentType"] = ImageAttachmentType,
            ["SortOrder"]      = sortOrder.ToString(),
        };

        return _api.PostFileAsync<UploadAttachmentResponse>(
            Base, fileStream, fileName, contentType,
            formFields: fields, formFieldName: "file", ct: ct);
    }

    /// <summary>DELETE /attachments/{id} (Attachment.Delete; hard delete, ownership-guarded).</summary>
    public Task<ApiResult> DeleteAsync(Guid attachmentId, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{attachmentId}", ct);

    /// <summary>PUT /attachments/primary (EntityImage.Update; ownership-guarded).</summary>
    public Task<ApiResult> SetPrimaryAsync(Guid blogId, Guid attachmentId, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/primary", new
        {
            EntityType   = BlogEntityType,
            EntityId     = blogId,
            AttachmentId = attachmentId,
        }, ct);

    /// <summary>PUT /attachments/reorder (Attachment.Update; ownership-guarded).</summary>
    public Task<ApiResult> ReorderAsync(Guid blogId, IReadOnlyList<Guid> orderedAttachmentIds, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/reorder", new
        {
            EntityType            = BlogEntityType,
            EntityId              = blogId,
            OrderedAttachmentIds  = orderedAttachmentIds,
        }, ct);
}
