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
        var url = $"{Base}?EntityType={TourEntityType}&EntityId={tourId}&AttachmentType={ImageAttachmentType}";

        return _api.PostFileAsync<UploadAttachmentResponse>(
            url, fileStream, fileName, contentType,
            formFieldName: "file",
            ct: ct);
    }

    // DELETE /api/v1/content-core/attachments/{attachmentId}
    public Task<ApiResult> DeleteImageAsync(Guid attachmentId, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{attachmentId}", ct);
}
