using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Packages;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum PackageOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError,
}

public sealed record PackageListResult(
    PackageOutcome Outcome,
    PackagesIndexVm? Data = null,
    string? Error = null);

public sealed record PackageManageResult(
    PackageOutcome Outcome,
    PackageManageVm? Data = null,
    string? Error = null);

public sealed record PackageActionResult(
    PackageOutcome Outcome,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    string? Error = null);

public sealed class PackagesFacade
{
    private const int DefaultPageSize = 20;

    private readonly PackagesApiClient _api;
    private readonly ProviderToursApiClient _tours;
    private readonly IOutputCacheStore _cache;
    private readonly IApiAssetUrlResolver _assetResolver;

    public PackagesFacade(
        PackagesApiClient api,
        ProviderToursApiClient tours,
        IOutputCacheStore cache,
        IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _tours = tours;
        _cache = cache;
        _assetResolver = assetResolver;
    }

    public async Task<PackageListResult> GetIndexAsync(int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        // API1: packages list + tour options (F10 included-tours picker) in parallel.
        var packagesTask = _api.GetPackagesAsync(page, DefaultPageSize, ct);
        var toursTask = _tours.GetMyToursAsync(1, 100, status: null, sort: null, ct);
        await Task.WhenAll(packagesTask, toursTask);

        var result = packagesTask.Result;
        if (result.IsUnauthorized) return new(PackageOutcome.ForceSignOut);
        if (result.IsForbidden) return new(PackageOutcome.Forbidden, Error: "You don't have access to packages.");
        if (!result.IsSuccess || result.Data is null)
            return new(PackageOutcome.ValidationError, Error: result.Error ?? "Could not load packages.");

        var vm = PackagesMapper.ToIndexVm(result.Data);

        // F10: soft-degrade — picker simply has no options when the lookup fails (ERR3).
        var tours = toursTask.Result;
        if (tours.IsSuccess && tours.Data is not null)
        {
            vm.TourOptions = tours.Data.Items
                .Select(t => new PackageTourOptionVm { Id = t.Id, Name = t.Name })
                .ToList();
        }

        return new(PackageOutcome.Ok, vm);
    }

    public async Task<PackageManageResult> GetManageAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);
        if (result.IsUnauthorized) return new(PackageOutcome.ForceSignOut);
        if (result.IsForbidden) return new(PackageOutcome.Forbidden, Error: "You don't have access to this package.");
        if (result.IsNotFound) return new(PackageOutcome.NotFound, Error: "Package not found.");
        if (!result.IsSuccess || result.Data is null)
            return new(PackageOutcome.ValidationError, Error: result.Error ?? "Could not load the package.");

        return new(PackageOutcome.Ok, PackagesMapper.ToManageVm(result.Data, _assetResolver));
    }

    public async Task<PackageActionResult> CreateAsync(CreatePackageFormVm vm, CancellationToken ct = default)
        => Normalize(await _api.CreateAsync(PackagesMapper.ToCreateRequest(vm), ct), "Could not create the package.");

    public async Task<PackageActionResult> AddInclusionAsync(Guid id, string description, CancellationToken ct = default)
    {
        var outcome = Normalize(
            await _api.AddInclusionAsync(id, new AddPackageInclusionApiRequest(description.Trim()), ct),
            "Could not add the inclusion.");
        if (outcome.Outcome == PackageOutcome.Ok) await _cache.EvictByTagAsync($"package:{id}", ct);
        return outcome;
    }

    public async Task<PackageActionResult> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        var outcome = Normalize(await _api.SubmitAsync(id, ct), "Could not submit the package for review.");
        if (outcome.Outcome == PackageOutcome.Ok) await _cache.EvictByTagAsync($"package:{id}", ct);
        return outcome;
    }

    public async Task<PackageActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var outcome = Normalize(await _api.DeleteAsync(id, ct), "Could not delete the package.");
        if (outcome.Outcome == PackageOutcome.Ok) await _cache.EvictByTagAsync($"package:{id}", ct);
        return outcome;
    }

    public async Task<PackageActionResult> UploadCoverAsync(Guid id, IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return new(PackageOutcome.ValidationError, Error: "Please choose an image to upload.");

        await using var stream = file.OpenReadStream();
        var outcome = Normalize(
            await _api.UploadCoverAsync(id, stream, file.FileName, file.ContentType, ct),
            "Could not upload the cover image.");
        if (outcome.Outcome == PackageOutcome.Ok) await _cache.EvictByTagAsync($"package:{id}", ct);
        return outcome;
    }

    public async Task<PackageActionResult> RemoveCoverAsync(Guid id, CancellationToken ct = default)
    {
        var outcome = Normalize(await _api.DeleteCoverAsync(id, ct), "Could not remove the cover image.");
        if (outcome.Outcome == PackageOutcome.Ok) await _cache.EvictByTagAsync($"package:{id}", ct);
        return outcome;
    }

    private static PackageActionResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return new(PackageOutcome.Ok);
        if (result.IsUnauthorized) return new(PackageOutcome.ForceSignOut);
        if (result.IsForbidden) return new(PackageOutcome.Forbidden,
            Error: "You don't have permission to manage packages.");
        if (result.IsNotFound) return new(PackageOutcome.NotFound, Error: "Package not found.");
        if (result.IsConflict) return new(PackageOutcome.Conflict,
            Error: result.Error ?? "This change isn't allowed in the package's current state.");
        if (result.IsValidationError) return new(PackageOutcome.ValidationError,
            ValidationErrors: result.ValidationErrors, Error: result.Error);
        return new(PackageOutcome.ValidationError, Error: result.Error ?? fallback);
    }
}
