using YallaJo.Web.Areas.Admin.Models.Attachments;
using YallaJo.Web.Areas.Admin.Models.Attachments;
using YallaJo.Web.Infrastructure.Api.Contracts;

using YallaJo.Web.Areas.Admin.ApiClients;
namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class AttachmentsFacade
{
    private readonly AttachmentsApiClient _api;
    public AttachmentsFacade(AttachmentsApiClient api) => _api = api;

    public async Task<ApiResult<AttachmentListVm>> GetForEntityAsync(
        AttachmentListFilterVm filter, CancellationToken ct = default)
    {
        var result = await _api.GetEntityAttachmentsAsync(filter.EntityType.ToString(), filter.EntityId, ct);

        if (result.IsSuccess)
        {
            var rows = (result.Data ?? []).Select(AttachmentsMapper.ToRowVm).ToList();
            return ApiResult<AttachmentListVm>.CreateSuccess(new AttachmentListVm
            {
                Filter      = filter,
                HasFilter   = true,
                Attachments = rows,
            });
        }

        if (result.IsUnauthorized) return ApiResult<AttachmentListVm>.ForceSignOut();
        return ApiResult<AttachmentListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult> UploadAsync(UploadAttachmentVm vm, CancellationToken ct = default)
    {
        if (vm.File is null || vm.File.Length == 0)
            return ApiResult.Invalid(new Dictionary<string, string[]>
            {
                [nameof(UploadAttachmentVm.File)] = ["File is required."],
            });

        await using var stream = vm.File.OpenReadStream();
        var fields = AttachmentsMapper.BuildUploadFields(vm);
        var result = await _api.UploadAsync(stream, vm.File.FileName, vm.File.ContentType, fields, ct);

        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? "Could not upload attachment.");
    }

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => Normalize(await _api.DeleteAsync(id, ct), "Could not delete attachment.");

    public async Task<ApiResult> SetPrimaryAsync(
        EntityTypeOption entityType, Guid entityId, Guid attachmentId, CancellationToken ct = default)
        => Normalize(await _api.SetPrimaryAsync(entityType.ToString(), entityId, attachmentId, ct),
            "Could not set primary image.");

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsNotFound)        return ApiResult.Fail("Not found.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.Error ?? fallback);
    }
}
