using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Areas.Provider.Models.TourPricing;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public enum TourPricingOutcome
{
    Ok,
    ForceSignOut,
    NotFound,
    Forbidden,
    Conflict,      
    ValidationError,
}

public sealed record TourPricingListResult(
    TourPricingOutcome Outcome,
    TourPricingIndexVm? Data = null,
    string? Error = null);

public sealed record TourPricingFormResult(
    TourPricingOutcome Outcome,
    TourPricingFormVm? Form = null,
    string? Error = null);

public sealed record TourPricingActionResult(
    TourPricingOutcome Outcome,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    string? Error = null);

public sealed class ProviderTourPricingFacade
{
    private readonly ProviderTourPricingApiClient _pricingApi;
    private readonly ProviderToursApiClient _toursApi;

    public ProviderTourPricingFacade(
        ProviderTourPricingApiClient pricingApi, ProviderToursApiClient toursApi)
    {
        _pricingApi = pricingApi;
        _toursApi = toursApi;
    }

    public async Task<TourPricingListResult> GetIndexAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourPricingOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourPricingOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourPricingOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        // activeOnly=false so the owner sees inactive tiers too.
        var tiers = await _pricingApi.GetPricingAsync(tourId, activeOnly: false, ct);
        if (tiers.IsUnauthorized) return new(TourPricingOutcome.ForceSignOut);
        if (tiers.IsForbidden) return new(TourPricingOutcome.Forbidden, Error: "You don't have access to this listing's pricing.");
        if (!tiers.IsSuccess || tiers.Data is null)
            return new(TourPricingOutcome.ValidationError, Error: tiers.Error ?? "Could not load pricing tiers.");

        var vm = TourPricingMapper.ToIndexVm(
            tourId, tour.Data.Name, Humanize(tour.Data.Status), tour.Data.Currency, tiers.Data);
        return new(TourPricingOutcome.Ok, vm);
    }

    public async Task<TourPricingFormResult> GetCreateAsync(Guid tourId, CancellationToken ct = default)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourPricingOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourPricingOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourPricingOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        return new(TourPricingOutcome.Ok, TourPricingMapper.ToCreateVm(tourId, tour.Data.Name, tour.Data.Currency));
    }

    public async Task<TourPricingActionResult> CreateAsync(Guid tourId, TourPricingFormVm vm, CancellationToken ct = default)
    {
        var result = await _pricingApi.CreateAsync(tourId, TourPricingMapper.ToCreateRequest(vm), ct);
        return NormalizeAction(result, "Could not create the pricing tier.");
    }

    public async Task<TourPricingFormResult> GetEditAsync(Guid tourId, Guid tierId, CancellationToken ct = default)
    {
        var tour = await _toursApi.GetByIdAsync(tourId, ct);
        if (tour.IsUnauthorized) return new(TourPricingOutcome.ForceSignOut);
        if (tour.IsForbidden) return new(TourPricingOutcome.Forbidden, Error: "You don't have access to this listing.");
        if (tour.IsNotFound || !tour.IsSuccess || tour.Data is null)
            return new(TourPricingOutcome.NotFound, Error: tour.Error ?? "Listing not found.");

        // The pricing endpoint has no get-by-id; fetch all (incl. inactive) and pick the tier.
        var tiers = await _pricingApi.GetPricingAsync(tourId, activeOnly: false, ct);
        if (tiers.IsUnauthorized) return new(TourPricingOutcome.ForceSignOut);
        if (!tiers.IsSuccess || tiers.Data is null)
            return new(TourPricingOutcome.ValidationError, Error: tiers.Error ?? "Could not load the pricing tier.");

        var tier = tiers.Data.FirstOrDefault(t => t.Id == tierId);
        if (tier is null) return new(TourPricingOutcome.NotFound, Error: "Pricing tier not found.");

        return new(TourPricingOutcome.Ok, TourPricingMapper.ToEditVm(tourId, tour.Data.Name, tour.Data.Currency, tier));
    }

    public async Task<TourPricingActionResult> UpdateAsync(Guid tourId, Guid tierId, TourPricingFormVm vm, CancellationToken ct = default)
    {
        var result = await _pricingApi.UpdateAsync(tourId, tierId, TourPricingMapper.ToUpdateRequest(vm), ct);
        return NormalizeAction(result, "Could not save the pricing tier.");
    }

    public async Task<TourPricingActionResult> DeleteAsync(Guid tourId, Guid tierId, CancellationToken ct = default)
    {
        var result = await _pricingApi.DeleteAsync(tourId, tierId, ct);
        return NormalizeAction(result, "Could not delete the pricing tier.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static TourPricingActionResult NormalizeAction(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return new(TourPricingOutcome.Ok);
        if (result.IsUnauthorized) return new(TourPricingOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourPricingOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's pricing.");
        if (result.IsNotFound) return new(TourPricingOutcome.NotFound, Error: "Pricing tier not found.");
        if (result.IsConflict) return new(TourPricingOutcome.Conflict,
            Error: result.Error ?? "This change isn't allowed — a tour must keep at least one active Adult pricing tier.");
        if (result.IsValidationError) return new(TourPricingOutcome.ValidationError,
            ValidationErrors: result.ValidationErrors, Error: result.Error);
        return new(TourPricingOutcome.ValidationError, Error: result.Error ?? fallback);
    }

    private static TourPricingActionResult NormalizeAction<T>(ApiResult<T> result, string fallback)
    {
        if (result.IsSuccess) return new(TourPricingOutcome.Ok);
        if (result.IsUnauthorized) return new(TourPricingOutcome.ForceSignOut);
        if (result.IsForbidden) return new(TourPricingOutcome.Forbidden,
            Error: "You don't have permission to manage this listing's pricing.");
        if (result.IsNotFound) return new(TourPricingOutcome.NotFound, Error: "Listing not found.");
        if (result.IsConflict) return new(TourPricingOutcome.Conflict,
            Error: result.Error ?? "This change isn't allowed in the listing's current state.");
        if (result.IsValidationError) return new(TourPricingOutcome.ValidationError,
            ValidationErrors: result.ValidationErrors, Error: result.Error);
        return new(TourPricingOutcome.ValidationError, Error: result.Error ?? fallback);
    }

    // "MoreDocsNeeded" → "More docs needed" (consistent with other provider screens).
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
