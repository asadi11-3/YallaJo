using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Staff;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessStaffFacade
{
    private readonly StaffApiClient _api;
    private readonly MyBusinessesApiClient _businesses;
    private readonly IOutputCacheStore _cache;

    public BusinessStaffFacade(StaffApiClient api, MyBusinessesApiClient businesses, IOutputCacheStore cache)
    {
        _api = api;
        _businesses = businesses;
        _cache = cache;
    }

    public async Task<ApiResult<StaffVm>> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        var detail = await _businesses.GetByIdAsync(businessId, ct);
        if (detail.IsUnauthorized)
        {
            return ApiResult<StaffVm>.ForceSignOut();
        }

        if (detail is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<StaffVm>.Fail(detail.StatusCode, detail.Error ?? "Could not load the business.");
        }

        var staff = await _api.GetStaffAsync(businessId, ct);
        if (staff.IsUnauthorized)
        {
            return ApiResult<StaffVm>.ForceSignOut();
        }

        if (staff is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<StaffVm>.Fail(staff.StatusCode, staff.Error ?? "Could not load the staff members.");
        }

        // D-5/API7: one batch ids= lookup enriches every row with a real identity.
        // Lookup failures are non-fatal — rows fall back to the raw UserId display.
        IReadOnlyDictionary<Guid, UserLookupItemResponse>? lookup = null;
        var userIds = staff.Data.Select(s => s.UserId).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var users = await _api.LookupUsersAsync(q: null, ids: userIds, ct);
            if (users is { IsSuccess: true, Data: not null })
            {
                lookup = users.Data.ToDictionary(u => u.Id);
            }
        }

        var vm = new StaffVm
        {
            BusinessId = businessId,
            BusinessName = detail.Data.Name,
            Status = detail.Data.Status,
            Staff = StaffMapper.ToRows(staff.Data, lookup),
        };

        return ApiResult<StaffVm>.Ok(vm);
    }

    /// <summary>Typeahead user search for the staff picker (F10). Returns normalized failures.</summary>
    public async Task<ApiResult<List<UserLookupItemResponse>>> LookupAsync(string q, CancellationToken ct = default)
    {
        var result = await _api.LookupUsersAsync(q, ids: null, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<List<UserLookupItemResponse>>.ForceSignOut();
        }

        return result;
    }

    public async Task<ApiResult> AddAsync(Guid businessId, AddStaffFormVm form, CancellationToken ct = default)
    {
        var request = new AddBusinessStaffApiRequest(form.UserId, form.Role);
        var apiResult = await _api.AddAsync(businessId, request, ct);
        var normalized = Normalize(apiResult, "Could not add the staff member.");
        if (normalized.IsSuccess) await _cache.EvictByTagAsync($"business:{businessId}", ct);
        return normalized;
    }

    public async Task<ApiResult> RemoveAsync(Guid businessId, Guid staffId, CancellationToken ct = default)
    {
        var apiResult = await _api.RemoveAsync(staffId, ct);
        var normalized = Normalize(apiResult, "Could not remove the staff member.");
        if (normalized.IsSuccess) await _cache.EvictByTagAsync($"business:{businessId}", ct);
        return normalized;
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsForbidden)
        {
            return ApiResult.Fail(403, "You do not own this business.");
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "The business or staff member was not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This person is already a staff member.");
        }

        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            return ApiResult.Invalid(result.ValidationErrors);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
