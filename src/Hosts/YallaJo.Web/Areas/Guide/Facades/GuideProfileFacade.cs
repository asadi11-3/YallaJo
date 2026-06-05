using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Profile;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>
/// Composes the guide profile read/update flows for the "My Profile" page.
/// </summary>
public sealed class GuideProfileFacade
{
    private readonly ProfileApiClient _api;
    private readonly ILogger<GuideProfileFacade> _logger;

    public GuideProfileFacade(ProfileApiClient api, ILogger<GuideProfileFacade> logger)
    {
        _api = api;
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

        var allSpecs = specsTask.Result;
        var allLangs = langsTask.Result;

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

        return await _api.UpdateProfileAsync(guideId.Value, request, ct);
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

    public Task<ApiResult> UploadAvatarAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        => UploadAndPersistAsync(fileStream, fileName, contentType, isAvatar: true, ct);

    public Task<ApiResult> UploadCoverAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
        => UploadAndPersistAsync(fileStream, fileName, contentType, isAvatar: false, ct);

    private async Task<ApiResult> UploadAndPersistAsync(
        Stream fileStream, string fileName, string contentType, bool isAvatar, CancellationToken ct)
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

        var url = upload.Data.Url!;
        return isAvatar
            ? await _api.UpdateAvatarAsync(url, ct)
            : await _api.UpdateCoverImageAsync(url, ct);
    }

    private async Task<Guid?> ResolveGuideIdAsync(CancellationToken ct)
    {
        var profileResult = await _api.GetMyProfileAsync(ct);
        return profileResult is { IsSuccess: true, Data: not null }
            ? profileResult.Data.Id
            : null;
    }

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
