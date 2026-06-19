using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Profile;
using YallaJo.Web.Areas.Guide.Services;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>
/// Composes the guide profile read/update flows for the "My Profile" page.
/// </summary>
public sealed class GuideProfileFacade
{
    private readonly ProfileApiClient _api;
    private readonly GuideIdAccessor _guideId;
    private readonly ILogger<GuideProfileFacade> _logger;

    public GuideProfileFacade(ProfileApiClient api, GuideIdAccessor guideId, ILogger<GuideProfileFacade> logger)
    {
        _api = api;
        _guideId = guideId;
        _logger = logger;
    }

    public async Task<ApiResult<ProfileVm>> GetAsync(CancellationToken ct = default)
    {
        var profileResult = await _api.GetMyProfileAsync(ct);
        if (profileResult.RequireSignOut)
        {
            return ApiResult<ProfileVm>.ForceSignOut();
        }

        if (!profileResult.IsSuccess || profileResult.Data is null)
        {
            return ApiResult<ProfileVm>.Fail(profileResult.StatusCode, profileResult.Error);
        }

        var profile = profileResult.Data;

        var specsTask = SafeSpecializationsAsync(ct);
        var langsTask = SafeLanguagesAsync(ct);
        await Task.WhenAll(specsTask, langsTask);

        // UI-PERF-R1: await the already-completed tasks (no .Result).
        var allSpecs = await specsTask;
        var allLangs = await langsTask;

        var langNameById = allLangs.ToDictionary(l => l.Id, l => l.Name);
        var specNameById = allSpecs.ToDictionary(s => s.Id, s => s.Name);

        var currentLanguageIds = profile.Languages.Select(l => l.LanguageId).ToHashSet();
        var currentSpecIds = profile.Specializations.Select(s => s.Id).ToHashSet();

        var vm = new ProfileVm
        {
            GuideId = profile.Id,
            DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? "Guide" : profile.DisplayName!,
            AvatarUrl = profile.AvatarUrl,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            TourCount = profile.TourCount,
            Form = new ProfileFormVm
            {
                Bio = profile.Bio,
                YearsOfExperience = profile.YearsOfExperience,
                HasFirstAid = profile.HasFirstAid,
                MoTALicenseNumber = profile.MoTALicenseNumber,
            },
            Languages = profile.Languages
                .Select(l => new GuideLanguageVm
                {
                    LanguageId = l.LanguageId,
                    Name = l.Name
                           ?? (langNameById.TryGetValue(l.LanguageId, out var n) ? n : "Language"),
                    Proficiency = l.Proficiency ?? "",
                })
                .ToList(),
            Specializations = profile.Specializations
                .Select(s => new GuideSpecializationVm
                {
                    Id = s.Id,
                    Name = s.Name
                           ?? (specNameById.TryGetValue(s.Id, out var n) ? n : "Specialization"),
                    Icon = s.Icon,
                })
                .ToList(),
            AvailableLanguages = allLangs
                .Where(l => !currentLanguageIds.Contains(l.Id))
                .Select(l => new OptionVm(l.Id, l.Name))
                .ToList(),
            AvailableSpecializations = allSpecs
                .Where(s => !currentSpecIds.Contains(s.Id))
                .Select(s => new OptionVm(s.Id, s.Name))
                .ToList(),
        };

        return ApiResult<ProfileVm>.Ok(vm);
    }

    public async Task<ApiResult> UpdateProfileAsync(ProfileFormVm form, CancellationToken ct = default)
    {
        var guideId = await ResolveGuideIdAsync(ct);
        if (guideId is null)
        {
            return ApiResult.Fail("Unable to resolve your guide profile.");
        }

        var request = new UpdateTourGuideProfileRequest(
            form.Bio,
            form.YearsOfExperience,
            form.HasFirstAid,
            string.IsNullOrWhiteSpace(form.MoTALicenseNumber) ? null : form.MoTALicenseNumber);

        var result = await _api.UpdateProfileAsync(guideId.Value, request, ct);
        if (result.IsSuccess)
        {
            // Profile fields feed the cached sidebar identity — drop the stale entry.
            _guideId.Invalidate();
        }

        return result;
    }

    public async Task<ApiResult> AddLanguageAsync(Guid languageId, string proficiency, CancellationToken ct = default)
    {
        var guideId = await ResolveGuideIdAsync(ct);
        if (guideId is null)
        {
            return ApiResult.Fail("Unable to resolve your guide profile.");
        }

        return await _api.AddLanguageAsync(
            guideId.Value, new AddTourGuideLanguageRequest(languageId, proficiency), ct);
    }

    public async Task<ApiResult> RemoveLanguageAsync(Guid languageId, CancellationToken ct = default)
    {
        var guideId = await ResolveGuideIdAsync(ct);
        if (guideId is null)
        {
            return ApiResult.Fail("Unable to resolve your guide profile.");
        }

        return await _api.RemoveLanguageAsync(guideId.Value, languageId, ct);
    }

    public async Task<ApiResult> AddSpecializationAsync(Guid specializationId, CancellationToken ct = default)
    {
        var guideId = await ResolveGuideIdAsync(ct);
        if (guideId is null)
        {
            return ApiResult.Fail("Unable to resolve your guide profile.");
        }

        return await _api.AddSpecializationAsync(
            guideId.Value, new AddTourGuideSpecializationRequest(specializationId), ct);
    }

    public async Task<ApiResult> RemoveSpecializationAsync(Guid specializationId, CancellationToken ct = default)
    {
        var guideId = await ResolveGuideIdAsync(ct);
        if (guideId is null)
        {
            return ApiResult.Fail("Unable to resolve your guide profile.");
        }

        return await _api.RemoveSpecializationAsync(guideId.Value, specializationId, ct);
    }

    public Task<ApiResult> DeactivateAsync(CancellationToken ct = default)
        => _api.DeactivateAsync(ct);

    public async Task<ApiResult> UploadAvatarAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var guideId = await ResolveGuideIdAsync(ct);
        if (guideId is null)
        {
            return ApiResult.Fail("Unable to resolve your guide profile.");
        }

        var upload = await _api.UploadImageAsync(guideId.Value, fileStream, fileName, contentType, ct);
        if (upload.RequireSignOut)
        {
            return ApiResult.ForceSignOut();
        }

        if (!upload.IsSuccess || upload.Data is null || string.IsNullOrWhiteSpace(upload.Data.Url))
        {
            return upload.IsValidationError
                ? ApiResult.ValidationFail(upload.StatusCode, upload.ValidationErrors!)
                : ApiResult.Fail(upload.StatusCode, upload.Error ?? "Image upload failed.");
        }

        var persist = await _api.UpdateAvatarAsync(upload.Data.Url!, ct);
        if (persist.IsSuccess)
        {
            // Avatar feeds the cached sidebar identity — drop the stale entry.
            _guideId.Invalidate();
        }

        return persist;
    }

    private Task<Guid?> ResolveGuideIdAsync(CancellationToken ct) =>
        // Delegates to the per-request memoized accessor (kills the GET /guides/me N+1 before every mutation).
        _guideId.GetGuideIdAsync(ct);

    private async Task<List<SpecializationResponse>> SafeSpecializationsAsync(CancellationToken ct)
    {
        try
        {
            var r = await _api.GetSpecializationsAsync(ct);
            return r is { IsSuccess: true, Data: not null } ? r.Data : [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load specializations for guide profile page.");
            return [];
        }
    }

    private async Task<List<LanguageResponse>> SafeLanguagesAsync(CancellationToken ct)
    {
        try
        {
            var r = await _api.GetLanguagesAsync(ct);
            return r is { IsSuccess: true, Data: not null } ? r.Data : [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load languages for guide profile page.");
            return [];
        }
    }
}
