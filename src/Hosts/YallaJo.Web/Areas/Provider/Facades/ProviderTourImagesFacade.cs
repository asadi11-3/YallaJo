using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.TourImages;
using YallaJo.Web.Services;
namespace YallaJo.Web.Areas.Provider.Facades;

public enum TourImagesOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    ValidationError,
}

public sealed record TourImagesListResult(
    TourImagesOutcome Outcome,
    TourImagesVm? Data = null,
    string? Error = null);

public sealed record TourImagesActionResult(
    TourImagesOutcome Outcome,
    string? Error = null);

public sealed class ProviderTourImagesFacade
{
    private readonly ProviderTourImagesApiClient _api;
    private readonly IOutputCacheStore _cache;
    private readonly IApiAssetUrlResolver _assetResolver;

    public ProviderTourImagesFacade(ProviderTourImagesApiClient api, IOutputCacheStore cache, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _cache = cache;
        _assetResolver = assetResolver;
    }

    public async Task<TourImagesListResult> GetIndexAsync(Guid tourId, CancellationToken ct = default)
    {
        var result = await _api.GetImagesAsync(tourId, ct);

        if (result.IsUnauthorized) return new(TourImagesOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourImagesOutcome.Forbidden,
            Error: "You don't have access to this listing's images.");
        if (result.IsNotFound) return new(TourImagesOutcome.NotFound, Error: "Listing not found.");
        if (!result.IsSuccess || result.Data is null)
            return new(TourImagesOutcome.ValidationError, Error: result.Error ?? "Could not load the listing images.");

        return new(TourImagesOutcome.Ok, TourImagesMapper.ToVm(tourId, result.Data, _assetResolver));
    }

    public async Task<TourImagesActionResult> UploadAsync(Guid tourId, TourImageUploadVm vm, CancellationToken ct = default)
    {
        // Defensive: controller ModelState already enforces this, but guard before touching the stream.
        if (vm.File is null || vm.File.Length == 0)
            return new(TourImagesOutcome.ValidationError, "Please choose an image to upload.");

        await using var stream = vm.File.OpenReadStream();
        var result = await _api.UploadImageAsync(tourId, stream, vm.File.FileName, vm.File.ContentType, ct);

        if (result.IsUnauthorized) return new(TourImagesOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourImagesOutcome.Forbidden,
            "You don't have permission to add images to this listing.");
        if (result.IsNotFound) return new(TourImagesOutcome.NotFound, "Listing not found.");
        if (result.IsValidationError)
            return new(TourImagesOutcome.ValidationError,
                result.Error ?? "The image was rejected. Use a JPG or PNG within the allowed size.");
        if (!result.IsSuccess)
            return new(TourImagesOutcome.ValidationError, result.Error ?? "Could not upload the image.");

        // Public detail page caches an image gallery — evict on every successful upload.
        await _cache.EvictByTagAsync($"tour:{tourId}", ct);
        return new(TourImagesOutcome.Ok);
    }

    public async Task<TourImagesActionResult> DeleteAsync(Guid tourId, Guid attachmentId, CancellationToken ct = default)
    {
        var result = await _api.DeleteImageAsync(attachmentId, ct);

        if (result.IsSuccess)
        {
            // Public detail page caches an image gallery — evict on every successful delete.
            await _cache.EvictByTagAsync($"tour:{tourId}", ct);
            return new(TourImagesOutcome.Ok);
        }
        if (result.IsUnauthorized) return new(TourImagesOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourImagesOutcome.Forbidden,
            "You don't have permission to delete this image.");
        if (result.IsNotFound) return new(TourImagesOutcome.NotFound, "Image not found.");
        return new(TourImagesOutcome.ValidationError, result.Error ?? "Could not delete the image.");
    }
}
