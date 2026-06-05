using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Staff;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessStaffFacade
{
    private readonly StaffApiClient _api;
    private readonly MyBusinessesApiClient _businesses;

    public BusinessStaffFacade(StaffApiClient api, MyBusinessesApiClient businesses)
    {
        _api = api;
        _businesses = businesses;
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

        var vm = new StaffVm
        {
            BusinessId = businessId,
            BusinessName = detail.Data.Name,
            Status = detail.Data.Status,
            Staff = StaffMapper.ToRows(staff.Data),
        };

        return ApiResult<StaffVm>.Ok(vm);
    }

    public async Task<ApiResult> AddAsync(Guid businessId, AddStaffFormVm form, CancellationToken ct = default)
    {
        var request = new AddBusinessStaffApiRequest(form.UserId, form.Role);
        var result = await _api.AddAsync(businessId, request, ct);
        return Normalize(result, "Could not add the staff member.");
    }

    public async Task<ApiResult> RemoveAsync(Guid staffId, CancellationToken ct = default)
    {
        var result = await _api.RemoveAsync(staffId, ct);
        return Normalize(result, "Could not remove the staff member.");
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
