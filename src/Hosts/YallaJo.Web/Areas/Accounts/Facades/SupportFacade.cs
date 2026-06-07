using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Support;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

/// <summary>
/// FE-1C — composes the user-facing Support Tickets pages.
/// <para>
/// List load <b>safe-degrades</b> like the FE-1B notifications inbox: any API failure
/// returns an empty list with a non-null <see cref="SupportListVm.LoadError"/> so the
/// page chrome always renders instead of 500-ing. Detail / reply / close map the API
/// outcomes (401/403/404/409/validation) to friendly messages.
/// </para>
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'Facade' suffix.
/// </summary>
public sealed class SupportFacade
{
    private const int DefaultPageSize = 20;

    private readonly SupportApiClient _api;
    private readonly ILogger<SupportFacade> _logger;

    public SupportFacade(SupportApiClient api, ILogger<SupportFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Builds the My Support Tickets list. Never throws: on any failure it returns an
    /// empty list with <see cref="SupportListVm.LoadError"/> set (except a 401, which
    /// the caller turns into a sign-out via <see cref="SupportListVm.LoadError"/> being
    /// null is NOT used here — see remarks). The 401 case is surfaced as a degraded list
    /// too, and the controller separately guards sign-out before rendering.
    /// </summary>
    public async Task<SupportListVm> GetListAsync(Guid? cursor, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.GetTicketsAsync(cursor, DefaultPageSize, ct);
            if (result is { IsSuccess: true, Data: not null })
            {
                return SupportMapper.ToListVm(result.Data);
            }

            if (result.IsUnauthorized)
            {
                return SupportMapper.DegradedListVm("Your session has expired. Please sign in again.");
            }

            return SupportMapper.DegradedListVm(result.Error ?? "Could not load your support tickets.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load support tickets list");
            return SupportMapper.DegradedListVm("Could not load your support tickets.");
        }
    }

    public async Task<ApiResult<SupportTicketDetailVm>> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.GetTicketAsync(id, ct);
            if (result is { IsSuccess: true, Data: not null })
            {
                return ApiResult<SupportTicketDetailVm>.Ok(SupportMapper.ToDetailVm(result.Data));
            }

            if (result.IsUnauthorized) return ApiResult<SupportTicketDetailVm>.ForceSignOut();
            if (result.IsForbidden)
                return ApiResult<SupportTicketDetailVm>.Fail(403, "You don't have access to this support ticket.");
            if (result.IsNotFound)
                return ApiResult<SupportTicketDetailVm>.Fail(404, "Support ticket not found.");

            return ApiResult<SupportTicketDetailVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load the support ticket.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load support ticket {TicketId}", id);
            return ApiResult<SupportTicketDetailVm>.Fail(500, "Could not load the support ticket.");
        }
    }

    public Task<ApiResult> PostMessageAsync(Guid id, string body, CancellationToken ct = default)
        => Normalize(_api.PostMessageAsync(id, body, ct), "Could not post your reply.");

    public Task<ApiResult> CloseAsync(Guid id, string? rowVersion, CancellationToken ct = default)
        => Normalize(_api.CloseAsync(id, rowVersion, ct), "Could not close the ticket.");

    private async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        try
        {
            var result = await call;
            if (result.IsSuccess) return ApiResult.Ok();
            if (result.IsUnauthorized) return ApiResult.ForceSignOut();
            if (result.IsForbidden) return ApiResult.Fail(403, "You don't have access to this support ticket.");
            if (result.IsNotFound) return ApiResult.Fail(404, "Support ticket not found.");
            if (result.IsConflict)
                return ApiResult.Fail(409, "This ticket was just updated (it may already be closed). Please reload and try again.");
            if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
            return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Support ticket mutation failed");
            return ApiResult.Fail(500, fallback);
        }
    }
}
