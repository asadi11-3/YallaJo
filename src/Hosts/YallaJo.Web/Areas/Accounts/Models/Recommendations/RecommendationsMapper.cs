namespace YallaJo.Web.Areas.Accounts.Models.Recommendations;

public static class RecommendationsMapper
{
    public static IReadOnlyList<RecommendationRowVm> ToRows(IReadOnlyList<RecommendationItemResponse> items) =>
        items.Select(ToRow).ToList();

    private static RecommendationRowVm ToRow(RecommendationItemResponse i) => new(
        i.Kind,
        i.Id,
        i.Name,
        i.Slug,
        i.BasePrice,
        i.Currency,
        i.AverageRating,
        i.BookingCount,
        i.IsFeatured,
        i.IsPinned,
        i.IsBoosted,
        i.BadgeText,
        ToSignals(i.SignalLabels));

    private static IReadOnlyList<SignalChipVm> ToSignals(IReadOnlyList<SignalLabelResponse> labels) =>
        labels.Select(l => new SignalChipVm(
            string.IsNullOrWhiteSpace(l.Title) ? l.Key : l.Title,
            l.Description)).ToList();

    public static PreferencesFormVm ToPreferencesForm(UserPreferencesResponse? p) => new()
    {
        BudgetTier = p?.BudgetTier,
        IsFamilyTraveler = p?.IsFamilyTraveler ?? false,
        CurrentTripStage = p?.CurrentTripStage,
    };
}
