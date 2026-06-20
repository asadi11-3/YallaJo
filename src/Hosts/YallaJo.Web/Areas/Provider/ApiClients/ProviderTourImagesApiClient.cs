using YallaJo.Web.Areas.Provider.Models.TourImages;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ProviderTourImagesApiClient
{
    private const string Base = "/api/v1/content-core/attachments";
    private const string TourEntityType = "Tour";
    private const string ImageAttachmentType = "Image";

    private readonly IApiClient _api;

    public ProviderTourImagesApiClient(IApiClient api) => _api = api;

    // GET /api/v1/content-core/attachments?entityType=Tour&entityId={tourId}
    public Task<ApiResult<List<AttachmentItemResponse>>> GetImagesAsync(Guid tourId, CancellationToken ct = default)
        => _api.GetAsync<List<AttachmentItemResponse>>(
            $"{Base}?entityType={TourEntityType}&entityId={tourId}", ct);

    public Task<ApiResult<UploadAttachmentResponse>> UploadImageAsync(
        Guid tourId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        // The ContentCore upload endpoint binds EntityType/EntityId/AttachmentType/SortOrder
        // from the multipart form body ([FromForm]), NOT the query string. Sending them as
        // query params produced "Required parameter 'string EntityType' was not provided from
        // form" (and likewise for the non-nullable "int SortOrder"). These values are derived
        // server-side from the route tourId (never the browser). SortOrder is required by the
        // endpoint; the server re-anchors ordering, so 0 (append) is a safe default.
        var fields = new Dictionary<string, string>
        {
            ["EntityType"]     = TourEntityType,
            ["EntityId"]       = tourId.ToString(),
            ["AttachmentType"] = ImageAttachmentType,
            ["SortOrder"]      = "0",
        };

        return _api.PostFileAsync<UploadAttachmentResponse>(
            Base, fileStream, fileName, contentType,
            formFields: fields,
            formFieldName: "file",
            ct: ct);
    }

    // DELETE /api/v1/content-core/attachments/{attachmentId}
    public Task<ApiResult> DeleteImageAsync(Guid attachmentId, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{attachmentId}", ct);
}
