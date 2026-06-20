using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Areas.Provider.Models.TourPricing;
using YallaJo.Web.Areas.Provider.Models.TourSchedules;
using YallaJo.Web.Areas.Provider.Models.TourImages;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum ProviderTourOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError 
}

public sealed record ProviderToursListResult(
    ProviderTourOutcome Outcome,
    ProviderToursIndexVm? Data = null,
    string? Error = null);

public sealed record ProviderTourDetailResult(
    ProviderTourOutcome Outcome,
    ProviderTourFormVm? Form = null,
    string? Status = null,
    string? Error = null);

public sealed record ProviderTourCreateResult(
    ProviderTourOutcome Outcome,
    Guid? TourId = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    string? Error = null);

public sealed record ProviderTourActionResult(
    ProviderTourOutcome Outcome,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    string? Error = null);

public sealed record ProviderTourReadinessResult(
    ProviderTourOutcome Outcome,
    TourReadinessVm? Readiness = null,
    string? Error = null);

public sealed class ProviderToursFacade
{
    private const int DefaultPageSize = 20;

    // Mirrors SubmitTourCommandHandler.MinDescriptionLength (backend authority).
    private const int MinDescriptionLength = 100;

    // Mirrors the backend "Adult" participant-type gate (HasActiveAdultPricingAsync).
    private const string AdultParticipantType = "Adult";

    private readonly ProviderToursApiClient _api;
    private readonly ProviderTourPricingApiClient _pricingApi;
    private readonly ProviderTourSchedulesApiClient _schedulesApi;
    private readonly ProviderTourImagesApiClient _imagesApi;
    private readonly IOutputCacheStore _cache;

    public ProviderToursFacade(
        ProviderToursApiClient api,
        ProviderTourPricingApiClient pricingApi,
        ProviderTourSchedulesApiClient schedulesApi,
        ProviderTourImagesApiClient imagesApi,
        IOutputCacheStore cache)
    {
        _api = api;
        _pricingApi = pricingApi;
        _schedulesApi = schedulesApi;
        _imagesApi = imagesApi;
        _cache = cache;
    }

    public async Task<ProviderToursListResult> GetIndexAsync(
        string? status, int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        // List + status counts fetched in parallel (API1); counts are a soft dependency (ERR3).
        var listTask = _api.GetMyToursAsync(page, DefaultPageSize, status, sort: null, ct);
        var countsTask = _api.GetStatusCountsAsync(ct);
        await Task.WhenAll(listTask, countsTask);

        var result = listTask.Result;

        if (result.IsUnauthorized) return new(ProviderTourOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderTourOutcome.Forbidden);
        if (!result.IsSuccess || result.Data is null)
            return new(ProviderTourOutcome.ValidationError,
                Error: result.Error ?? "Could not load your listings.");

        var vm = ProviderToursMapper.ToIndexVm(result.Data, status);

        var counts = countsTask.Result;
        if (counts.IsSuccess && counts.Data is not null)
        {
            vm = new ProviderToursIndexVm
            {
                Items = vm.Items,
                Status = vm.Status,
                Page = vm.Page,
                PageSize = vm.PageSize,
                Total = vm.Total,
                TotalPages = vm.TotalPages,
                StatusOptions = vm.StatusOptions,
                StatusCounts = new TourStatusCountsVm
                {
                    Draft = counts.Data.Draft,
                    Pending = counts.Data.Pending,
                    Approved = counts.Data.Approved,
                    Rejected = counts.Data.Rejected,
                    Suspended = counts.Data.Suspended,
                    Archived = counts.Data.Archived,
                    Total = counts.Data.Total,
                },
            };
        }

        return new(ProviderTourOutcome.Ok, vm);
    }

    public async Task<ProviderTourDetailResult> GetEditAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);

        if (result.IsUnauthorized) return new(ProviderTourOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderTourOutcome.Forbidden);
        if (result.IsNotFound) return new(ProviderTourOutcome.NotFound, Error: "Listing not found.");
        if (!result.IsSuccess || result.Data is null)
            return new(ProviderTourOutcome.ValidationError, Error: result.Error ?? "Could not load the listing.");

        return new(ProviderTourOutcome.Ok, ProviderToursMapper.ToFormVm(result.Data), result.Data.Status);
    }

    /// <summary>
    /// Computes submit-readiness for a draft listing from PERSISTED data, mirroring the
    /// backend pre-submit gate. Fetches detail + active pricing + active schedules + images
    /// in parallel. Sub-resource lookups are a soft dependency: if one fails we mark the
    /// readiness as degraded rather than reporting a false "incomplete".
    /// </summary>
    public async Task<ProviderTourReadinessResult> GetReadinessAsync(Guid id, CancellationToken ct = default)
    {
        var detailTask = _api.GetByIdAsync(id, ct);
        var pricingTask = _pricingApi.GetPricingAsync(id, activeOnly: true, ct);
        var schedulesTask = _schedulesApi.GetSchedulesAsync(id, activeOnly: true, ct);
        var imagesTask = _imagesApi.GetImagesAsync(id, ct);

        await Task.WhenAll(detailTask, pricingTask, schedulesTask, imagesTask);

        var detail = detailTask.Result;
        if (detail.IsUnauthorized) return new(ProviderTourOutcome.ForceSignOut);
        if (detail.IsForbidden) return new(ProviderTourOutcome.Forbidden);
        if (detail.IsNotFound) return new(ProviderTourOutcome.NotFound, Error: "Listing not found.");
        if (!detail.IsSuccess || detail.Data is null)
            return new(ProviderTourOutcome.ValidationError, Error: detail.Error ?? "Could not load the listing.");

        var pricing = pricingTask.Result;
        var schedules = schedulesTask.Result;
        var images = imagesTask.Result;

        // A sub-resource that failed to load leaves us unable to assert its requirement.
        var degraded = !pricing.IsSuccess || !schedules.IsSuccess || !images.IsSuccess;

        var readiness = ComputeReadiness(
            id,
            description: detail.Data.Description,
            placeId: detail.Data.PlaceId,
            meetingPointLatitude: detail.Data.MeetingPointLatitude,
            meetingPointLongitude: detail.Data.MeetingPointLongitude,
            pricing: pricing.IsSuccess ? pricing.Data : null,
            schedules: schedules.IsSuccess ? schedules.Data : null,
            images: images.IsSuccess ? images.Data : null,
            degraded: degraded);

        return new(ProviderTourOutcome.Ok, readiness);
    }

    /// <summary>
    /// Pure, unit-testable readiness projection. Mirrors SubmitTourCommandHandler:
    /// Basics = Place linked + meeting point present + description >= 100 chars;
    /// Pricing = at least one active tier AND at least one active Adult tier;
    /// Schedule = at least one active schedule; Images = at least one image.
    /// When a collection is <c>null</c> (lookup failed), that requirement is treated
    /// as unmet but the result is flagged <see cref="TourReadinessVm.IsDegraded"/>.
    /// </summary>
    internal static TourReadinessVm ComputeReadiness(
        Guid tourId,
        string? description,
        Guid? placeId,
        decimal? meetingPointLatitude,
        decimal? meetingPointLongitude,
        IReadOnlyList<TourPricingTierResponse>? pricing,
        IReadOnlyList<TourScheduleResponse>? schedules,
        IReadOnlyList<AttachmentItemResponse>? images,
        bool degraded)
    {
        var hasPlace = placeId is { } pid && pid != Guid.Empty;
        var hasMeetingPoint = meetingPointLatitude.HasValue && meetingPointLongitude.HasValue;
        var hasDescription = !string.IsNullOrWhiteSpace(description)
                             && description.Trim().Length >= MinDescriptionLength;
        var basicsComplete = hasPlace && hasMeetingPoint && hasDescription;

        var activeTiers = pricing?.Where(p => p.IsActive).ToList() ?? [];
        var hasActivePricing = activeTiers.Count > 0;
        var hasAdultPricing = activeTiers.Any(p =>
            string.Equals(p.ParticipantType, AdultParticipantType, StringComparison.OrdinalIgnoreCase));
        var pricingComplete = hasActivePricing && hasAdultPricing;

        var scheduleComplete = schedules?.Any(s => s.IsActive) ?? false;
        var imagesComplete = images is { Count: > 0 };

        // Ordered missing-requirement checklist (resource keys resolved in the view).
        var missing = new List<string>();
        if (!basicsComplete)
        {
            if (!hasPlace) missing.Add("Provider.TourReadiness.MissingPlace");
            if (!hasMeetingPoint) missing.Add("Provider.TourReadiness.MissingMeetingPoint");
            if (!hasDescription) missing.Add("Provider.TourReadiness.MissingDescription");
        }
        if (!hasActivePricing) missing.Add("Provider.TourReadiness.MissingPricing");
        else if (!hasAdultPricing) missing.Add("Provider.TourReadiness.MissingAdultPricing");
        if (!scheduleComplete) missing.Add("Provider.TourReadiness.MissingSchedule");
        if (!imagesComplete) missing.Add("Provider.TourReadiness.MissingImages");

        return new TourReadinessVm
        {
            TourId = tourId,
            BasicsComplete = basicsComplete,
            PricingComplete = pricingComplete,
            ScheduleComplete = scheduleComplete,
            ImagesComplete = imagesComplete,
            IsDegraded = degraded,
            MissingRequirementKeys = missing,
        };
    }

    public async Task<ProviderTourCreateResult> CreateAsync(ProviderTourFormVm vm, CancellationToken ct = default)
    {
        var result = await _api.CreateAsync(ProviderToursMapper.ToCreateRequest(vm), ct);

        if (result.IsUnauthorized) return new(ProviderTourOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderTourOutcome.Forbidden,
            Error: "Only approved providers can create listings.");
        if (result.IsValidationError)
            return new(ProviderTourOutcome.ValidationError,
                ValidationErrors: result.ValidationErrors, Error: result.Error);
        if (result.IsConflict)
            return new(ProviderTourOutcome.Conflict, Error: result.Error ?? "A listing with this slug or name already exists.");
        if (!result.IsSuccess || result.Data is null)
            return new(ProviderTourOutcome.ValidationError, Error: result.Error ?? "Could not create the listing.");

        // Evict the public detail tag in case a stale entry exists (e.g., a previously rejected tour
        // with the same id). For brand-new tours this is a safe no-op.
        await _cache.EvictByTagAsync($"tour:{result.Data.TourId}", ct);
        return new(ProviderTourOutcome.Ok, result.Data.TourId);
    }

    public async Task<ProviderTourActionResult> UpdateAsync(Guid id, ProviderTourFormVm vm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vm.RowVersion) || !TryParseRowVersion(vm.RowVersion, out var rowVersion))
            return new(ProviderTourOutcome.Conflict,
                Error: "The listing version is missing. Please reload the page and try again.");

        var result = await _api.UpdateAsync(id, ProviderToursMapper.ToUpdateRequest(vm, rowVersion), ct);
        return await EvictOnOkAsync(NormalizeAction(result, "Could not save the listing."), id, ct);
    }

    public async Task<ProviderTourActionResult> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        var (ok, rowVersion, failure) = await GetFreshRowVersionAsync(id, ct);
        if (!ok) return failure!;

        var result = await _api.SubmitAsync(id, rowVersion!, ct);
        return await EvictOnOkAsync(NormalizeAction(result, "Could not submit the listing."), id, ct);
    }

    public async Task<ProviderTourActionResult> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var (ok, rowVersion, failure) = await GetFreshRowVersionAsync(id, ct);
        if (!ok) return failure!;

        var result = await _api.ArchiveAsync(id, rowVersion!, ct);
        return await EvictOnOkAsync(NormalizeAction(result, "Could not archive the listing."), id, ct);
    }

    // DELETE /tours/{id} is a soft-delete on the API and does not require a RowVersion.
    public async Task<ProviderTourActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.DeleteAsync(id, ct);
        return await EvictOnOkAsync(NormalizeAction(result, "Could not delete the listing."), id, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    // Fetches the owner detail to obtain a fresh RowVersion for submit/archive (the list
    // page doesn't carry one). Returns a normalized failure if it can't be resolved.
    private async Task<(bool Ok, byte[]? RowVersion, ProviderTourActionResult? Failure)> GetFreshRowVersionAsync(
        Guid id, CancellationToken ct)
    {
        var detail = await _api.GetByIdAsync(id, ct);

        if (detail.IsUnauthorized) return (false, null, new(ProviderTourOutcome.ForceSignOut));
        if (detail.IsForbidden) return (false, null, new(ProviderTourOutcome.Forbidden, Error: "You do not have access to this listing."));
        if (detail.IsNotFound) return (false, null, new(ProviderTourOutcome.NotFound, Error: "Listing not found."));
        if (!detail.IsSuccess || detail.Data is null)
            return (false, null, new(ProviderTourOutcome.ValidationError, Error: detail.Error ?? "Could not load the listing."));
        if (detail.Data.RowVersion is not { Length: > 0 } rowVersion)
            return (false, null, new(ProviderTourOutcome.Conflict,
                Error: "The listing version could not be determined. Please reload and try again."));

        return (true, rowVersion, null);
    }

    private static ProviderTourActionResult NormalizeAction(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return new(ProviderTourOutcome.Ok);
        if (result.IsUnauthorized) return new(ProviderTourOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderTourOutcome.Forbidden,
            Error: "You don't have permission to perform this action.");
        if (result.IsNotFound) return new(ProviderTourOutcome.NotFound, Error: "Listing not found.");
        if (result.IsConflict) return new(ProviderTourOutcome.Conflict,
            Error: result.Error ?? "This listing was modified or is in a state that doesn't allow this action. Please reload and try again.");
        if (result.IsValidationError) return new(ProviderTourOutcome.ValidationError,
            ValidationErrors: result.ValidationErrors, Error: result.Error);
        return new(ProviderTourOutcome.ValidationError, Error: result.Error ?? fallback);
    }

    private static bool TryParseRowVersion(string base64, out byte[] rowVersion)
    {
        try { rowVersion = Convert.FromBase64String(base64); return rowVersion.Length > 0; }
        catch (FormatException) { rowVersion = []; return false; }
    }

    // Evicts the public detail cache tag for the affected tour when the action succeeded.
    // Plan §3 rule #3: facades must evict output-cache tags after successful writes.
    private async Task<ProviderTourActionResult> EvictOnOkAsync(ProviderTourActionResult result, Guid tourId, CancellationToken ct)
    {
        if (result.Outcome == ProviderTourOutcome.Ok)
            await _cache.EvictByTagAsync($"tour:{tourId}", ct);
        return result;
    }
}
