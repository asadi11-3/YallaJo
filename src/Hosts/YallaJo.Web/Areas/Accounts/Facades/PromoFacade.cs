using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Promo;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.Facades;

/// <summary>
/// Application-facing facade for My Profile promotional placements.
/// Auto-registered as scoped by FeatureServiceRegistration (name ends in "Facade").
/// </summary>
public sealed class PromoFacade
{
    /// <summary>The four placements rendered on the My Profile page, in display order.</summary>
    public static readonly IReadOnlyList<string> ProfilePlacementKeys =
    [
        "MyProfile.RightRail.Top",
        "MyProfile.RightRail.Middle",
        "MyProfile.RightRail.Bottom",
        "MyProfile.BottomBanner",
    ];

    private readonly PromoApiClient _api;
    private readonly IApiAssetUrlResolver _assetResolver;

    public PromoFacade(PromoApiClient api, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _assetResolver = assetResolver;
    }

    /// <summary>
    /// Loads the My Profile promo blocks. When <paramref name="includeInactive"/> is true
    /// (Admin/SuperAdmin/Owner), inactive/empty placements are included so editors can manage them.
    /// </summary>
    public async Task<ApiResult<IReadOnlyList<PromoBlockVm>>> GetForProfileAsync(
        bool includeInactive,
        CancellationToken ct = default)
    {
        var result = includeInactive
            ? await _api.GetAdminAsync(ProfilePlacementKeys, ct)
            : await _api.GetPublicAsync(ProfilePlacementKeys, ct);

        if (result.IsSuccess && result.Data is not null)
        {
            var blocks = result.Data
                .Select(MapToVm)
                .ToList();
            return ApiResult<IReadOnlyList<PromoBlockVm>>.CreateSuccess(blocks);
        }

        if (result.IsUnauthorized)
        {
            return ApiResult<IReadOnlyList<PromoBlockVm>>.ForceSignOut();
        }

        return ApiResult<IReadOnlyList<PromoBlockVm>>.CreateFailure(
            result.StatusCode,
            result.Error ?? "Unable to load promotional content.");
    }

    /// <summary>Update a placement's content fields. Returns the refreshed block on success.</summary>
    public async Task<ApiResult<PromoBlockVm>> UpdateAsync(
        string key,
        UpdatePromoBlockVm vm,
        CancellationToken ct = default)
    {
        var request = new UpdatePromoBlockRequest(
            vm.Title,
            vm.Description,
            vm.ButtonText,
            vm.ButtonUrl,
            vm.BadgeText,
            vm.IconName,
            vm.IsActive,
            vm.SortOrder,
            vm.StartsAt,
            vm.EndsAt);

        var result = await _api.UpdateAsync(key, request, ct);
        return MapEnvelope(result);
    }

    /// <summary>Upload/replace a placement's image. Returns the refreshed block on success.</summary>
    public async Task<ApiResult<PromoBlockVm>> UploadImageAsync(
        string key,
        IFormFile? file,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
        {
            return ApiResult<PromoBlockVm>.ValidationFail(400, new Dictionary<string, string[]>
            {
                ["file"] = ["An image file is required."],
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await _api.UploadImageAsync(key, stream, file.FileName, file.ContentType, ct);
        return MapEnvelope(result);
    }

    private ApiResult<PromoBlockVm> MapEnvelope(ApiResult<PromoBlockEnvelope> result)
    {
        if (result.IsSuccess && result.Data?.PromoBlock is not null)
        {
            return ApiResult<PromoBlockVm>.CreateSuccess(MapToVm(result.Data.PromoBlock));
        }

        if (result.IsUnauthorized)
        {
            return ApiResult<PromoBlockVm>.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult<PromoBlockVm>.CreateFailure(404, result.Error ?? "Placement not found.");
        }

        if (result.IsValidationError)
        {
            return ApiResult<PromoBlockVm>.ValidationFail(
                result.StatusCode,
                result.ValidationErrors!);
        }

        return ApiResult<PromoBlockVm>.CreateFailure(
            result.StatusCode,
            result.Error ?? "Unable to save promotional content.");
    }

    private PromoBlockVm MapToVm(PromoBlockResponse src) => new()
    {
        PlacementKey = src.PlacementKey,
        Title = src.Title,
        Description = src.Description,
        ImageUrl = string.IsNullOrWhiteSpace(src.ImageUrl)
            ? null
            : _assetResolver.Resolve(src.ImageUrl),
        ButtonText = src.ButtonText,
        ButtonUrl = src.ButtonUrl,
        BadgeText = src.BadgeText,
        IconName = src.IconName,
        IsActive = src.IsActive,
        SortOrder = src.SortOrder,
        StartsAt = src.StartsAt,
        EndsAt = src.EndsAt,
    };
}
