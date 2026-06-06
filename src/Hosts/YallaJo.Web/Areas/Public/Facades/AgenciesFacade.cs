using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Agencies;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class AgenciesFacade(AgenciesApiClient api)
{
    private const int PageSize = 12;

    public async Task<ApiResult<AgenciesGridVm>> GetGridAsync(int page, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        var result = await api.GetAgenciesAsync(pageNumber, PageSize, ct);
        if (result is not { IsSuccess: true, Data: { } data })
        {
            return ApiResult<AgenciesGridVm>.Fail(result.StatusCode, result.Error ?? "Could not load agencies.");
        }

        var cards = data.Agencies
            .Select(a => new AgencyCardVm
            {
                UserId = a.UserId,
                BusinessName = string.IsNullOrWhiteSpace(a.BusinessName) ? "Agency" : a.BusinessName,
                Description = a.Description,
            })
            .ToList();

        // GetAgenciesResult only echoes TotalCount; derive paging from the request.
        return ApiResult<AgenciesGridVm>.Ok(new AgenciesGridVm
        {
            Agencies = cards,
            PageNumber = pageNumber,
            PageSize = PageSize,
            TotalCount = data.TotalCount,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = (long)pageNumber * PageSize < data.TotalCount,
        });
    }

    public async Task<ApiResult<AgencyDetailVm>> GetDetailAsync(Guid agencyUserId, CancellationToken ct = default)
    {
        var result = await api.GetAgencyAsync(agencyUserId, ct);
        if (result is not { IsSuccess: true, Data: { } d })
        {
            return ApiResult<AgencyDetailVm>.Fail(result.StatusCode, result.Error ?? "Agency not found.");
        }

        return ApiResult<AgencyDetailVm>.Ok(new AgencyDetailVm
        {
            UserId = d.UserId,
            BusinessName = string.IsNullOrWhiteSpace(d.BusinessName) ? "Agency" : d.BusinessName,
            ContactEmail = d.ContactEmail,
            ContactPhone = d.ContactPhone,
            Address = d.Address,
            Description = d.Description,
            ProviderType = d.ProviderType,
            ActiveGuideCount = d.ActiveGuideCount,
        });
    }

    public Task<ApiResult> ApplyAsync(Guid agencyUserId, CancellationToken ct = default)
        => api.ApplyAsync(agencyUserId, ct);
}
