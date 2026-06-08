using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Packages;
using YallaJo.Web.Infrastructure.Api.Contracts;

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
    private readonly IOutputCacheStore _cache;

    public PackagesFacade(PackagesApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<PackageListResult> GetIndexAsync(int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var result = await _api.GetPackagesAsync(page, DefaultPageSize, ct);
        if (result.IsUnauthorized) return new(PackageOutcome.ForceSignOut);
        if (result.IsForbidden) return new(PackageOutcome.Forbidden, Error: "You don't have access to packages.");
        if (!result.IsSuccess || result.Data is null)
            return new(PackageOutcome.ValidationError, Error: result.Error ?? "Could not load packages.");

        return new(PackageOutcome.Ok, PackagesMapper.ToIndexVm(result.Data));
    }

    public async Task<PackageManageResult> GetManageAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);
        if (result.IsUnauthorized) return new(PackageOutcome.ForceSignOut);
        if (result.IsForbidden) return new(PackageOutcome.Forbidden, Error: "You don't have access to this package.");
        if (result.IsNotFound) return new(PackageOutcome.NotFound, Error: "Package not found.");
        if (!result.IsSuccess || result.Data is null)
            return new(PackageOutcome.ValidationError, Error: result.Error ?? "Could not load the package.");

        return new(PackageOutcome.Ok, PackagesMapper.ToManageVm(result.Data));
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
