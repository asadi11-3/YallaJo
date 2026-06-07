using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.TourGuides;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum TourGuideOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError,
}

public sealed record TourGuideListResult(
    TourGuideOutcome Outcome,
    TourGuidesIndexVm? Data = null,
    string? Error = null);

public sealed record TourGuideActionResult(
    TourGuideOutcome Outcome,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    string? Error = null);

public sealed class ProviderTourGuidesFacade
{
    private readonly ProviderTourGuidesApiClient _guidesApi;
    private readonly ProviderToursApiClient _toursApi;

    public ProviderTourGuidesFacade(
        ProviderTourGuidesApiClient guidesApi, ProviderToursApiClient toursApi)
    {
        _guidesApi = guidesApi;
        _toursApi = toursApi;
    }

    public async Task<TourGuideListResult> GetIndexAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourGuideOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourGuideOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourGuideOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        var guides = await _guidesApi.GetGuidesAsync(tourId, ct);
        if (guides.IsUnauthorized) return new(TourGuideOutcome.ForceSignOut);
        if (guides.IsForbidden) return new(TourGuideOutcome.Forbidden, Error: "You don't have access to this listing's guides.");
        if (!guides.IsSuccess || guides.Data is null)
            return new(TourGuideOutcome.ValidationError, Error: guides.Error ?? "Could not load assigned guides.");

        var vm = TourGuidesMapper.ToIndexVm(tourId, tour.Data.Name, Humanize(tour.Data.Status), guides.Data);
        return new(TourGuideOutcome.Ok, vm);
    }

    public async Task<TourGuideActionResult> AssignAsync(Guid tourId, AssignTourGuideFormVm vm, CancellationToken ct = default)
    {
        var result = await _guidesApi.AssignAsync(tourId, TourGuidesMapper.ToAssignRequest(vm), ct);
        return NormalizeAction(result, "Could not assign the guide.");
    }

    public async Task<TourGuideActionResult> RemoveAsync(Guid tourId, Guid guideUserId, CancellationToken ct = default)
    {
        var result = await _guidesApi.RemoveAsync(tourId, guideUserId, ct);
        return NormalizeAction(result, "Could not remove the guide.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static TourGuideActionResult NormalizeAction(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return new(TourGuideOutcome.Ok);
        if (result.IsUnauthorized) return new(TourGuideOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourGuideOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's guides.");
        if (result.IsNotFound) return new(TourGuideOutcome.NotFound, Error: "Guide or listing not found.");
        if (result.IsConflict) return new(TourGuideOutcome.Conflict,
            Error: result.Error ?? "This guide is already assigned, or the listing's state doesn't allow this change.");
        if (result.IsValidationError) return new(TourGuideOutcome.ValidationError,
            ValidationErrors: result.ValidationErrors, Error: result.Error);
        return new(TourGuideOutcome.ValidationError, Error: result.Error ?? fallback);
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new System.Text.StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1])) sb.Append(' ');
            sb.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
