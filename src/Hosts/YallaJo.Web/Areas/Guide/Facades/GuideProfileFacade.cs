using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideProfileFacade
{
    private readonly GuideApiClient _api;

    public GuideProfileFacade(GuideApiClient api) => _api = api;

    public async Task<ApiResult<GuideProfileVm>> GetAsync(CancellationToken ct = default)
    {
        var profile = await _api.GetMyProfileAsync(ct);
        if (profile.IsUnauthorized)
        {
            return ApiResult<GuideProfileVm>.ForceSignOut();
        }

        if (profile is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<GuideProfileVm>.Ok(new GuideProfileVm { IsGuide = false });
        }

        var p = profile.Data;
        var vm = new GuideProfileVm
        {
            IsGuide = true,
            GuideId = p.Id,
            DisplayName = p.DisplayName,
            AvatarUrl = p.AvatarUrl,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            TourCount = p.TourCount,
            Languages = p.Languages
                .Select(l => l.Name ?? l.Code ?? string.Empty)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList(),
            Specializations = p.Specializations
                .Select(s => s.Name ?? string.Empty)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList(),
            Edit = new EditGuideProfileVm
            {
                Bio = p.Bio ?? string.Empty,
                YearsOfExperience = p.YearsOfExperience,
                HasFirstAid = p.HasFirstAid,
                MoTALicenseNumber = p.MoTALicenseNumber
            }
        };

        return ApiResult<GuideProfileVm>.Ok(vm);
    }

    public Task<ApiResult> UpdateAsync(Guid guideId, EditGuideProfileVm form, CancellationToken ct = default)
    {
        var license = string.IsNullOrWhiteSpace(form.MoTALicenseNumber) ? null : form.MoTALicenseNumber.Trim();
        var request = new UpdateTourGuideProfileRequest(
            form.Bio.Trim(),
            form.YearsOfExperience,
            form.HasFirstAid,
            license);

        return NormalizeAsync(() => _api.UpdateProfileAsync(guideId, request, ct), "Could not update your profile.");
    }

    private static async Task<ApiResult> NormalizeAsync(Func<Task<ApiResult>> call, string fallback)
    {
        ApiResult result;
        try
        {
            result = await call();
        }
        catch
        {
            return ApiResult.Fail(500, fallback);
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsSuccess)
        {
            return ApiResult.Ok(result.StatusCode);
        }

        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
