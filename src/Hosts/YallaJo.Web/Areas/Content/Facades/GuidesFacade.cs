using YallaJo.Web.Areas.Content.ApiClients;
using YallaJo.Web.Areas.Content.Models.Guides;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.Facades;

public sealed class GuidesFacade
{
    private const int TourPageSize = 20;

    private readonly GuidesApiClient _guides;
    private readonly SpecializationsApiClient _specializations;
    private readonly IApiAssetUrlResolver _assetResolver;

    public GuidesFacade(
        GuidesApiClient guides,
        SpecializationsApiClient specializations,
        IApiAssetUrlResolver assetResolver)
    {
        _guides = guides;
        _specializations = specializations;
        _assetResolver = assetResolver;
    }

    public async Task<ApiResult<GuideListVm>> GetListAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var guidesTask = _guides.ListGuidesAsync(page, pageSize, ct);
        var specsTask = _specializations.ListSpecializationsAsync(ct);

        await Task.WhenAll(guidesTask, specsTask);

        var guidesResult = await guidesTask;
        var specsResult = await specsTask;

        if (guidesResult.IsUnauthorized)
            return ApiResult<GuideListVm>.ForceSignOut();

        if (!guidesResult.IsSuccess || guidesResult.Data is null)
            return ApiResult<GuideListVm>.Fail(
                guidesResult.StatusCode, guidesResult.Error ?? "Could not load guides.");

        var data = guidesResult.Data;

        var specs = specsResult is { IsSuccess: true, Data: { } sd }
            ? sd.Where(s => s.IsActive).ToList()
            : [];

        var specNameLookup = specs.ToDictionary(s => s.Id, s => s.Name);
        var specVms = specs.Select(GuidesMapper.ToSpecializationVm).ToList();

        var pageNumber = data.Page <= 0 ? page : data.Page;
        var effectivePageSize = data.PageSize <= 0 ? pageSize : data.PageSize;
        var totalPages = effectivePageSize > 0
            ? (int)Math.Ceiling(data.TotalCount / (double)effectivePageSize)
            : 0;

        var vm = new GuideListVm
        {
            Guides          = data.Items
                .Select(g => GuidesMapper.ToCardVm(g, specNameLookup, _assetResolver))
                .ToList(),
            Specializations = specVms,
            Pager           = new GuidePagerVm
            {
                PageNumber      = pageNumber,
                PageSize        = effectivePageSize,
                TotalCount      = data.TotalCount,
                TotalPages      = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage     = pageNumber < totalPages,
            },
        };

        return ApiResult<GuideListVm>.Ok(vm);
    }

    public async Task<ApiResult<GuideDetailsVm>> GetDetailsAsync(string slug, CancellationToken ct = default)
    {
        var guideResult = await _guides.GetGuideBySlugAsync(slug, ct);

        if (guideResult.IsUnauthorized)
            return ApiResult<GuideDetailsVm>.ForceSignOut();
        if (guideResult.IsNotFound)
            return ApiResult<GuideDetailsVm>.Fail(404, "Guide not found.");
        if (!guideResult.IsSuccess || guideResult.Data is null)
            return ApiResult<GuideDetailsVm>.Fail(
                guideResult.StatusCode, guideResult.Error ?? "Could not load the guide.");

        var guide = guideResult.Data;

        var toursTask = _guides.ListGuideToursAsync(guide.Id, 1, TourPageSize, ct);
        var specsTask = _specializations.ListSpecializationsAsync(ct);

        await Task.WhenAll(toursTask, specsTask);

        var toursResult = await toursTask;
        var specsResult = await specsTask;

        var tours = toursResult is { IsSuccess: true, Data: { } td }
            ? td.Items.Select(GuidesMapper.ToTourVm).ToList()
            : [];

        var specLookup = specsResult is { IsSuccess: true, Data: { } sd }
            ? sd.Where(s => s.IsActive).ToDictionary(s => s.Id, GuidesMapper.ToSpecializationVm)
            : new Dictionary<Guid, SpecializationVm>();

        var guideSpecVms = guide.Specializations
            .Select(s => specLookup.TryGetValue(s.SpecializationId, out var v) ? v : null)
            .Where(v => v is not null)
            .Select(v => v!)
            .ToList();

        var vm = GuidesMapper.ToDetailsVm(guide, slug, guideSpecVms, tours, _assetResolver);
        return ApiResult<GuideDetailsVm>.Ok(vm);
    }
}
