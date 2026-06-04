using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.NotificationTemplates;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class NotificationTemplatesFacade
{
    private readonly NotificationTemplatesApiClient _api;

    public NotificationTemplatesFacade(NotificationTemplatesApiClient api) => _api = api;

    public async Task<ApiResult<NotificationTemplatesListVm>> GetIndexAsync(CancellationToken ct)
    {
        var result = await _api.GetAllAsync(ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<NotificationTemplatesListVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<NotificationTemplatesListVm>.Fail(
                result.StatusCode,
                result.Error ?? "Could not load notification templates.");
        }

        return ApiResult<NotificationTemplatesListVm>.Ok(NotificationTemplatesMapper.ToListVm(result.Data));
    }

    public async Task<ApiResult<NotificationTemplateEditVm>> GetEditAsync(Guid id, CancellationToken ct)
    {
        var result = await _api.GetAllAsync(ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<NotificationTemplateEditVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<NotificationTemplateEditVm>.Fail(
                result.StatusCode,
                result.Error ?? "Could not load notification templates.");
        }

        var template = result.Data.FirstOrDefault(x => x.Id == id);
        if (template is null)
        {
            return ApiResult<NotificationTemplateEditVm>.Fail(404, "Notification template not found.");
        }

        return ApiResult<NotificationTemplateEditVm>.Ok(NotificationTemplatesMapper.ToEditVm(template));
    }

    public Task<ApiResult> CreateAsync(CreateTemplateRequest req, CancellationToken ct)
        => Normalize(_api.CreateAsync(req, ct), "Could not create the notification template.");

    public Task<ApiResult> UpdateAsync(Guid id, UpdateTemplateRequest req, CancellationToken ct)
        => Normalize(_api.UpdateAsync(id, req, ct), "Could not update the notification template.");

    public Task<ApiResult> DeleteAsync(Guid id, string? rowVersion, CancellationToken ct)
        => Normalize(_api.DeleteAsync(id, rowVersion, ct), "Could not delete the notification template.");

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "Notification template not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This template was modified by someone else. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
