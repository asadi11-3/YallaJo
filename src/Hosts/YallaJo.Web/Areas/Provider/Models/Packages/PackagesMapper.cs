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

    public static PackageManageVm ToManageVm(PackageDetailResponse d) => new()
    {
        Id              = d.Id,
        Name            = d.Name,
        Description     = d.Description,
        PriceAmount     = d.PriceAmount,
        Currency        = d.Currency,
        MaxParticipants = d.MaxParticipants,
        IsActive        = d.IsActive,
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
        IncludedTourIds: ParseGuids(vm.IncludedTourIds),
        Inclusions:      ParseLines(vm.Inclusions));

    // Comma/space/newline-separated GUIDs → distinct list (ignores invalid tokens).
    private static IReadOnlyCollection<Guid> ParseGuids(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(t => Guid.TryParse(t.Trim(), out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();
    }

    private static IReadOnlyCollection<string> ParseLines(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }
}
