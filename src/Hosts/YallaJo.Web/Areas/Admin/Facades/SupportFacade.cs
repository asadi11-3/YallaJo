using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Support;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class SupportFacade
{
    private const int DefaultPageSize = 20;
    private readonly SupportApiClient _api;

    public SupportFacade(SupportApiClient api) => _api = api;

    public async Task<ApiResult<SupportListVm>> GetIndexAsync(
        string? status, string? category, Guid? cursor, int pageSize, CancellationToken ct)
    {
        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = DefaultPageSize;
        }

        var result = await _api.GetTicketsAsync(status, category, cursor, pageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<SupportListVm>.ForceSignOut();
        }
        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<SupportListVm>.Fail(result.StatusCode, result.Error ?? "Could not load support tickets.");
        }

        return ApiResult<SupportListVm>.Ok(SupportMapper.ToListVm(result.Data, status, category));
    }

    public async Task<ApiResult<SupportTicketDetailVm>> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var result = await _api.GetTicketAsync(id, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<SupportTicketDetailVm>.ForceSignOut();
        }
        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<SupportTicketDetailVm>.Fail(result.StatusCode, result.Error ?? "Could not load the support ticket.");
        }

        return ApiResult<SupportTicketDetailVm>.Ok(SupportMapper.ToDetailVm(result.Data));
    }

    public Task<ApiResult> PostMessageAsync(Guid id, string body, bool isInternal, CancellationToken ct)
        => Normalize(_api.PostMessageAsync(id, body, isInternal, ct), "Could not post the reply.");

    public Task<ApiResult> CloseAsync(Guid id, string? rowVersion, CancellationToken ct)
        => Normalize(_api.CloseAsync(id, rowVersion, ct), "Could not close the ticket.");

    public Task<ApiResult> AssignAsync(Guid id, Guid adminUserId, CancellationToken ct)
        => Normalize(_api.AssignAsync(id, adminUserId, ct), "Could not assign the ticket.");

    public Task<ApiResult> ResolveAsync(Guid id, string? notes, string? rowVersion, CancellationToken ct)
        => Normalize(_api.ResolveAsync(id, notes, rowVersion, ct), "Could not resolve the ticket.");

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }
        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "Support ticket not found.");
        }
        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This ticket was modified by someone else. Please reload and try again.");
        }
        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
