using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.Models.Packages;

public static class PackagesMapper
{
    public static PackagesIndexVm ToIndexVm(PackageListResponse r) => new()
    {
        Packages    = r.Items.Select(ToRowVm).ToList(),
        Page        = r.PageNumber,
        PageSize    = r.PageSize,
        TotalCount  = r.TotalCount,
        HasPrevious = r.HasPreviousPage,
        HasNext     = r.HasNextPage,
    };

    private static PackageRowVm ToRowVm(PackageSummaryResponse p) => new()
    {
        Id                = p.Id,
        Name              = p.Name,
        PriceAmount       = p.PriceAmount,
        Currency          = p.Currency,
        IncludedTourCount = p.IncludedTourCount,
        ValidFrom         = p.ValidFrom,
        ValidTo           = p.ValidTo,
        CreatedAt         = p.CreatedAt,
    };

    public static PackageManageVm ToManageVm(PackageDetailResponse d, IApiAssetUrlResolver assetResolver) => new()
    {
        Id              = d.Id,
        Name            = d.Name,
        Description     = d.Description,
        PriceAmount     = d.PriceAmount,
        Currency        = d.Currency,
        MaxParticipants = d.MaxParticipants,
        IsActive        = d.IsActive,
        CoverImageUrl   = assetResolver.Resolve(d.CoverImageUrl),
        IncludedTours   = d.IncludedTours,
        Inclusions      = d.Inclusions.OrderBy(i => i.SortOrder).ToList(),
    };

    public static CreateTourPackageApiRequest ToCreateRequest(CreatePackageFormVm vm) => new(
        Name:            vm.Name.Trim(),
        Description:     string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
        Price:           vm.Price,
        Currency:        vm.Currency.Trim().ToUpperInvariant(),
        MaxParticipants: vm.MaxParticipants,
        ValidFrom:       vm.ValidFrom,
        ValidTo:         vm.ValidTo,
        IncludedTourIds: vm.IncludedTourIds.Distinct().ToList(),
        Inclusions:      ParseLines(vm.Inclusions));

    private static IReadOnlyCollection<string> ParseLines(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }
}
