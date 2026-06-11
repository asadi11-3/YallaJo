using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Proposals;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>
/// Builds the guide "My Proposals" view model and handles create/submit actions.
/// </summary>
public sealed class GuideProposalsFacade
{
    private readonly ProposalsApiClient _api;
    private readonly ILogger<GuideProposalsFacade> _logger;

    public GuideProposalsFacade(ProposalsApiClient api, ILogger<GuideProposalsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<ProposalsVm>> GetAsync(CancellationToken ct = default)
    {
        var rows = new List<ProposalRowVm>();

        // UI-PERF-API1: fetch proposals and the place options (F10 picker prefill) in parallel.
        var placesTask = FetchPlaceOptionsAsync(ct);

        try
        {
            var result = await _api.GetMyProposalsAsync(ct);
            if (result.RequireSignOut)
            {
                return ApiResult<ProposalsVm>.ForceSignOut();
            }

            if (result is { IsSuccess: true, Data: not null })
            {
                rows = result.Data
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProposalRowVm(
                        p.Id,
                        p.Title,
                        p.ShortDescription,
                        p.Status,
                        p.BasePrice,
                        p.Currency,
                        p.CreatedAt))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            // Degrade gracefully (UI-ERR3): never block the page on the list call.
            _logger.LogWarning(ex, "Failed to load tour proposals list");
        }

        var places = await placesTask;
        return ApiResult<ProposalsVm>.Ok(new ProposalsVm { Proposals = rows, Places = places });
    }

    /// <summary>Place lookup mapped to picker options (F10) — serves both the SSR prefill and the async combobox proxy.</summary>
    public async Task<ApiResult<IReadOnlyList<PlaceOptionVm>>> GetPlaceOptionsAsync(string? term, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.LookupPlacesAsync(term, ct);
            if (result.RequireSignOut)
            {
                return ApiResult<IReadOnlyList<PlaceOptionVm>>.ForceSignOut();
            }

            if (!result.IsSuccess || result.Data is null)
            {
                return ApiResult<IReadOnlyList<PlaceOptionVm>>.Fail(result.StatusCode, result.Error);
            }

            var options = result.Data
                .Select(p => new PlaceOptionVm(
                    p.Id,
                    string.IsNullOrWhiteSpace(p.City) ? p.Name : $"{p.Name} — {p.City}"))
                .ToList();
            return ApiResult<IReadOnlyList<PlaceOptionVm>>.Ok(options);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to look up places (term='{Term}').", term);
            return ApiResult<IReadOnlyList<PlaceOptionVm>>.Fail(500, "Unable to load places.");
        }
    }

    private async Task<IReadOnlyList<PlaceOptionVm>> FetchPlaceOptionsAsync(CancellationToken ct)
    {
        // UI-ERR3: picker options are an enhancement — degrade to an empty list on failure.
        // Never throws (the task may be abandoned on the sign-out early-return path).
        try
        {
            var result = await GetPlaceOptionsAsync(term: null, ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    public async Task<ApiResult<Guid>> CreateAsync(CreateProposalFormVm form, CancellationToken ct = default)
    {
        var request = new CreateTourProposalRequest(
            form.Title.Trim(),
            form.Description.Trim(),
            NullIfBlank(form.ShortDescription),
            form.PlaceId,
            form.DurationMinutes,
            form.MaxGroupSize,
            form.BasePrice,
            NormalizeCurrency(form.Currency),
            form.RequestExclusive);

        try
        {
            return await _api.CreateAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create tour proposal");
            return ApiResult<Guid>.Fail(500, "Unable to create the proposal. Please try again.");
        }
    }

    public async Task<ApiResult> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _api.SubmitAsync(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to submit tour proposal {ProposalId}", id);
            return ApiResult.Fail(500, "Unable to submit the proposal. Please try again.");
        }
    }

    private static string NormalizeCurrency(string? currency)
        => string.IsNullOrWhiteSpace(currency) ? "JOD" : currency.Trim().ToUpperInvariant();

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
