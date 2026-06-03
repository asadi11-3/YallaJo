namespace YallaJo.Web.Areas.Provider.Models.TourPricing;

public static class TourPricingMapper
{
    public static TourPricingIndexVm ToIndexVm(
        Guid tourId, string tourName, string tourStatusLabel, string currency,
        IReadOnlyList<TourPricingTierResponse> tiers) => new()
    {
        TourId          = tourId,
        TourName        = tourName,
        TourStatusLabel = tourStatusLabel,
        Currency        = currency,
        Tiers           = tiers
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.ParticipantType)
            .Select(ToRowVm)
            .ToList(),
    };

    private static TourPricingRowVm ToRowVm(TourPricingTierResponse t) => new()
    {
        Id                   = t.Id,
        Name                 = t.Name,
        ParticipantType      = t.ParticipantType,
        ParticipantTypeLabel = Humanize(t.ParticipantType),
        IsAdult              = IsAdult(t.ParticipantType),
        Price                = t.Price,
        Currency             = t.Currency,
        MinParticipants      = t.MinParticipants,
        MaxParticipants      = t.MaxParticipants,
        IsActive             = t.IsActive,
    };

    public static TourPricingFormVm ToCreateVm(Guid tourId, string tourName, string currency) => new()
    {
        TourId      = tourId,
        TourName    = tourName,
        Currency    = currency,
        ParticipantType = "Adult", // guide providers toward the required Adult tier
        IsActive    = true,
    };

    public static TourPricingFormVm ToEditVm(
        Guid tourId, string tourName, string currency, TourPricingTierResponse t) => new()
    {
        TourId          = tourId,
        TierId          = t.Id,
        TourName        = tourName,
        Name            = t.Name,
        Description     = t.Description,
        Price           = t.Price,
        Currency        = string.IsNullOrWhiteSpace(t.Currency) ? currency : t.Currency,
        ParticipantType = string.IsNullOrWhiteSpace(t.ParticipantType) ? "Adult" : t.ParticipantType,
        MinParticipants = t.MinParticipants,
        MaxParticipants = t.MaxParticipants,
        IsActive        = t.IsActive,
    };

    public static CreateTourPricingTierApiRequest ToCreateRequest(TourPricingFormVm vm) => new(
        Name:            vm.Name.Trim(),
        Description:     string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
        Price:           vm.Price,
        Currency:        vm.Currency.Trim().ToUpperInvariant(),
        ParticipantType: vm.ParticipantType,
        MinParticipants: vm.MinParticipants,
        MaxParticipants: vm.MaxParticipants);

    public static UpdateTourPricingTierApiRequest ToUpdateRequest(TourPricingFormVm vm) => new(
        Name:            vm.Name.Trim(),
        Description:     string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
        Price:           vm.Price,
        Currency:        vm.Currency.Trim().ToUpperInvariant(),
        ParticipantType: vm.ParticipantType,
        MinParticipants: vm.MinParticipants,
        MaxParticipants: vm.MaxParticipants,
        IsActive:        vm.IsActive);

    private static bool IsAdult(string participantType) =>
        string.Equals(participantType, "Adult", StringComparison.OrdinalIgnoreCase);

    private static string Humanize(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value;
}
