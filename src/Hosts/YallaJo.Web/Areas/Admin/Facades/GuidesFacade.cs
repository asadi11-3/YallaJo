using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Guides;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class GuidesFacade
{
    private readonly GuidesApiClient _api;

    public GuidesFacade(GuidesApiClient api) => _api = api;

    public async Task<ApiResult<GuidesVm>> GetIndexAsync(Guid? id, CancellationToken ct)
    {
        if (id is not { } guideId || guideId == Guid.Empty)
        {
            return ApiResult<GuidesVm>.Ok(new GuidesVm());
        }

        var result = await _api.GetGuideAsync(guideId, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<GuidesVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<GuidesVm>.Fail(result.StatusCode, result.Error ?? "Could not load the guide profile.");
        }

        return ApiResult<GuidesVm>.Ok(GuidesMapper.ToVm(result.Data));
    }

    public Task<ApiResult> SuspendAsync(Guid guideId, string reason, CancellationToken ct)
        => Normalize(_api.SuspendAsync(guideId, reason, ct), "Could not suspend the tour guide.");

    public Task<ApiResult> ReinstateAsync(Guid guideId, CancellationToken ct)
        => Normalize(_api.ReinstateAsync(guideId, ct), "Could not reinstate the tour guide.");

    // §8.14 — update a guide profile.
    public Task<ApiResult> UpdateAsync(AdminEditGuideVm form, CancellationToken ct)
    {
        if (form.Id == Guid.Empty)
        {
            return Task.FromResult(ApiResult.Fail(400, "A valid guide is required."));
        }

        var request = new AdminUpdateGuideApiRequest(
            string.IsNullOrWhiteSpace(form.Bio) ? null : form.Bio.Trim(),
            form.YearsOfExperience,
            form.HasFirstAid,
            string.IsNullOrWhiteSpace(form.MoTALicenseNumber) ? null : form.MoTALicenseNumber.Trim());

        return Normalize(_api.UpdateAsync(form.Id, request, ct), "Could not update the tour guide profile.");
    }

    // §8.14 — deactivate (delete) a guide.
    public Task<ApiResult> DeleteAsync(Guid guideId, CancellationToken ct)
        => guideId == Guid.Empty
            ? Task.FromResult(ApiResult.Fail(400, "A valid guide is required."))
            : Normalize(_api.DeleteAsync(guideId, ct), "Could not deactivate the tour guide.");

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
            return ApiResult.Fail(404, "Tour guide not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the guide's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
