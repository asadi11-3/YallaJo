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

        return ApiResult<ProposalsVm>.Ok(new ProposalsVm { Proposals = rows });
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
