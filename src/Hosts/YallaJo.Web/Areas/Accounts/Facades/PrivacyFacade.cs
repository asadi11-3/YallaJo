using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

/// <summary>
/// FE-1D — composes the Privacy &amp; Data page actions: data export (GDPR portability),
/// request analytics-data deletion, and cancel a pending deletion.
/// <para>
/// Because there is no pending-status read endpoint, the two friendly states are mapped
/// from the action outcomes: a request-deletion that returns 400 (<c>Gdpr.AlreadyPending</c>)
/// means a deletion is already scheduled; a cancel that returns 404 (<c>Gdpr.NotFound</c>)
/// means there is nothing pending to cancel.
/// </para>
/// Auto-registered (scoped-self) by AddFeatureServices() via the 'Facade' suffix.
/// </summary>
public sealed class PrivacyFacade
{
    private readonly PrivacyApiClient _api;
    private readonly ILogger<PrivacyFacade> _logger;

    public PrivacyFacade(PrivacyApiClient api, ILogger<PrivacyFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the user's data export (raw JSON bytes) for the BFF to re-serve as a
    /// file download. Failures are normalized; 401 surfaces as a forced sign-out.
    /// </summary>
    public async Task<ApiResult<ApiFile>> ExportAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _api.ExportAsync(ct);
            if (result is { IsSuccess: true, Data: not null })
            {
                return result;
            }

            if (result.IsUnauthorized) return ApiResult<ApiFile>.ForceSignOut();

            return ApiResult<ApiFile>.Fail(
                result.StatusCode, result.Error ?? "Could not prepare your data export. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Privacy data export failed");
            return ApiResult<ApiFile>.Fail(500, "Could not prepare your data export. Please try again.");
        }
    }

    /// <summary>
    /// Requests deletion of all personal analytics/recommendation data. Maps
    /// 400 (Gdpr.AlreadyPending) to a friendly "already scheduled" message.
    /// </summary>
    public async Task<ApiResult> RequestDataDeletionAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _api.RequestDataDeletionAsync(ct);
            if (result.IsSuccess) return ApiResult.Ok();
            if (result.IsUnauthorized) return ApiResult.ForceSignOut();
            if (result.IsForbidden) return ApiResult.Fail(403, "You don't have permission to do this.");
            if (result.IsValidationError)
            {
                // The only documented validation failure here is Gdpr.AlreadyPending.
                return ApiResult.Fail(400,
                    "A deletion of your recommendation data is already scheduled. You can cancel it below within the 30-day window.");
            }

            return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not request data deletion. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Privacy data-deletion request failed");
            return ApiResult.Fail(500, "Could not request data deletion. Please try again.");
        }
    }

    /// <summary>
    /// Cancels a pending analytics-data deletion. Maps 404 (Gdpr.NotFound) to a friendly
    /// "no pending deletion" message.
    /// </summary>
    public async Task<ApiResult> CancelDataDeletionAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _api.CancelDataDeletionAsync(ct);
            if (result.IsSuccess) return ApiResult.Ok();
            if (result.IsUnauthorized) return ApiResult.ForceSignOut();
            if (result.IsForbidden) return ApiResult.Fail(403, "You don't have permission to do this.");
            if (result.IsNotFound)
            {
                return ApiResult.Fail(404, "You have no pending data deletion to cancel.");
            }

            return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not cancel the deletion. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Privacy data-deletion cancel failed");
            return ApiResult.Fail(500, "Could not cancel the deletion. Please try again.");
        }
    }
}
