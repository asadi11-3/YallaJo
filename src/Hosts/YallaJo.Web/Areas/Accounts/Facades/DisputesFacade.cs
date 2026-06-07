using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Disputes;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

/// <summary>
/// §3.8 Disputes facade. Composes the user's disputes (<c>GET /disputes/my</c>) with the
/// payments they can dispute (reusing <see cref="PaymentsFacade"/> →
/// <c>GET /payments/my-payments</c>) and maps <see cref="ApiResult{T}"/> to the page VM.
/// <para>
/// The backend returns the full dispute list (no server paging), so paging is applied
/// here per the D1 convention: 1-based page, default 20 / max 50, pager driven only by
/// HasPrevious/HasNext.
/// </para>
/// </summary>
public sealed class DisputesFacade
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 50;

    private readonly DisputesApiClient _api;
    private readonly PaymentsFacade _payments;

    public DisputesFacade(DisputesApiClient api, PaymentsFacade payments)
    {
        _api = api;
        _payments = payments;
    }

    public async Task<ApiResult<DisputesVm>> GetAsync(int page, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        const int pageSize = DefaultPageSize;

        // Parallel reads: the user's disputes + their payments (for the open-dispute form).
        var disputesTask = _api.GetMyDisputesAsync(ct);
        var paymentsTask = _payments.GetAsync(ct);
        await Task.WhenAll(disputesTask, paymentsTask);

        var disputesResult = await disputesTask;
        var paymentsResult = await paymentsTask;

        if (disputesResult.RequireSignOut || paymentsResult.RequireSignOut)
            return ApiResult<DisputesVm>.ForceSignOut();

        if (!disputesResult.IsSuccess || disputesResult.Data is null)
            return ApiResult<DisputesVm>.Fail(
                disputesResult.StatusCode, disputesResult.Error ?? "Could not load your disputes.");

        var ordered = disputesResult.Data
            .OrderByDescending(d => d.CreatedAt)
            .ToList();

        var paged = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(DisputesMapper.ToRow)
            .ToList();

        // Disputable payments (best-effort: a failed payments read just hides the form,
        // it never fails the whole page — ERR2).
        var disputablePayments = paymentsResult is { IsSuccess: true, Data: not null }
            ? paymentsResult.Data.Payments
                .Where(DisputesMapper.IsDisputable)
                .Select(DisputesMapper.ToDisputable)
                .ToList()
            : [];

        return ApiResult<DisputesVm>.Ok(new DisputesVm
        {
            Disputes = paged,
            DisputablePayments = disputablePayments,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = ordered.Count,
        });
    }

    public async Task<ApiResult> OpenAsync(OpenDisputeFormVm form, CancellationToken ct = default)
    {
        var request = new OpenDisputeApiRequest(
            form.PaymentId,
            form.Reason.Trim(),
            form.Description.Trim());

        var result = await _api.OpenAsync(request, ct);

        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsForbidden) return ApiResult.Fail(403, "You can only dispute your own payments.");
        if (result.IsNotFound) return ApiResult.Fail(404, "That payment could not be found.");
        if (result.IsConflict)
            return ApiResult.Fail(409, result.Error ?? "A dispute already exists for this payment.");
        if (result.IsValidationError && result.ValidationErrors is { Count: > 0 })
            return ApiResult.Invalid(result.ValidationErrors);
        if (result.StatusCode is 422 or 400)
            return ApiResult.Fail(422, result.Error ?? "This payment cannot be disputed.");
        if (!result.IsSuccess)
            return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not open the dispute.");

        return ApiResult.Ok();
    }
}
