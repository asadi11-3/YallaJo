using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.TourApplications;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum TourApplicationOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,
    ValidationError,
}

public sealed record TourApplicationListResult(
    TourApplicationOutcome Outcome,
    TourApplicationsIndexVm? Data = null,
    string? Error = null);

public sealed record TourApplicationActionResult(
    TourApplicationOutcome Outcome,
    string? Error = null);

public sealed class ProviderTourApplicationsFacade
{
    private const int DefaultPageSize = 20;

    private readonly ProviderTourApplicationsApiClient _applicationsApi;
    private readonly ProviderToursApiClient _toursApi;

    public ProviderTourApplicationsFacade(
        ProviderTourApplicationsApiClient applicationsApi, ProviderToursApiClient toursApi)
    {
        _applicationsApi = applicationsApi;
        _toursApi = toursApi;
    }

    public async Task<TourApplicationListResult> GetIndexAsync(Guid tourId, int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourApplicationOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourApplicationOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourApplicationOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        var apps = await _applicationsApi.GetApplicationsAsync(tourId, page, DefaultPageSize, ct);
        if (apps.IsUnauthorized) return new(TourApplicationOutcome.ForceSignOut);
        if (apps.IsForbidden) return new(TourApplicationOutcome.Forbidden, Error: "You don't have access to this listing's applications.");
        if (!apps.IsSuccess || apps.Data is null)
            return new(TourApplicationOutcome.ValidationError, Error: apps.Error ?? "Could not load applications.");

        var vm = TourApplicationsMapper.ToIndexVm(tourId, tour.Data.Name, Humanize(tour.Data.Status), apps.Data);
        return new(TourApplicationOutcome.Ok, vm);
    }

    public async Task<TourApplicationActionResult> ApproveAsync(Guid tourId, Guid applicationId, CancellationToken ct = default)
        => Normalize(await _applicationsApi.ApproveAsync(tourId, applicationId, ct), "Could not approve the application.");

    public async Task<TourApplicationActionResult> RejectAsync(Guid tourId, Guid applicationId, string reason, CancellationToken ct = default)
        => Normalize(await _applicationsApi.RejectAsync(tourId, applicationId, new RejectGuideApplicationApiRequest(reason), ct),
                     "Could not reject the application.");

    public async Task<TourApplicationActionResult> OpenAsync(Guid tourId, CancellationToken ct = default)
        => Normalize(await _applicationsApi.OpenApplicationsAsync(tourId, ct), "Could not open applications.");

    public async Task<TourApplicationActionResult> CloseAsync(Guid tourId, CancellationToken ct = default)
        => Normalize(await _applicationsApi.CloseApplicationsAsync(tourId, ct), "Could not close applications.");

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static TourApplicationActionResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return new(TourApplicationOutcome.Ok);
        if (result.IsUnauthorized) return new(TourApplicationOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourApplicationOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's applications.");
        if (result.IsNotFound) return new(TourApplicationOutcome.NotFound, Error: "Application or listing not found.");
        if (result.IsConflict) return new(TourApplicationOutcome.Conflict,
            Error: result.Error ?? "This application can no longer be changed in its current state.");
        if (result.IsValidationError) return new(TourApplicationOutcome.ValidationError,
            Error: result.Error ?? fallback);
        return new(TourApplicationOutcome.ValidationError, Error: result.Error ?? fallback);
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
