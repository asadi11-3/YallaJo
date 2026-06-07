using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Tours;
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

public sealed class ProviderToursFacade
{
    private const int DefaultPageSize = 20;

    private readonly ProviderToursApiClient _api;

    public ProviderToursFacade(ProviderToursApiClient api) => _api = api;

    public async Task<ProviderToursListResult> GetIndexAsync(
        string? status, int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var result = await _api.GetMyToursAsync(page, DefaultPageSize, status, sort: null, ct);

        if (result.IsUnauthorized) return new(ProviderTourOutcome.ForceSignOut);
        if (result.IsForbidden) return new(ProviderTourOutcome.Forbidden);
        if (!result.IsSuccess || result.Data is null)
            return new(ProviderTourOutcome.ValidationError,
                Error: result.Error ?? "Could not load your listings.");

        return new(ProviderTourOutcome.Ok, ProviderToursMapper.ToIndexVm(result.Data, status));
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

        return new(ProviderTourOutcome.Ok, result.Data.TourId);
    }

    public async Task<ProviderTourActionResult> UpdateAsync(Guid id, ProviderTourFormVm vm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vm.RowVersion) || !TryParseRowVersion(vm.RowVersion, out var rowVersion))
            return new(ProviderTourOutcome.Conflict,
                Error: "The listing version is missing. Please reload the page and try again.");

        var result = await _api.UpdateAsync(id, ProviderToursMapper.ToUpdateRequest(vm, rowVersion), ct);
        return NormalizeAction(result, "Could not save the listing.");
    }

    public async Task<ProviderTourActionResult> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        var (ok, rowVersion, failure) = await GetFreshRowVersionAsync(id, ct);
        if (!ok) return failure!;

        var result = await _api.SubmitAsync(id, rowVersion!, ct);
        return NormalizeAction(result, "Could not submit the listing.");
    }

    public async Task<ProviderTourActionResult> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var (ok, rowVersion, failure) = await GetFreshRowVersionAsync(id, ct);
        if (!ok) return failure!;

        var result = await _api.ArchiveAsync(id, rowVersion!, ct);
        return NormalizeAction(result, "Could not archive the listing.");
    }

    // DELETE /tours/{id} is a soft-delete on the API and does not require a RowVersion.
    public async Task<ProviderTourActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.DeleteAsync(id, ct);
        return NormalizeAction(result, "Could not delete the listing.");
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
}
