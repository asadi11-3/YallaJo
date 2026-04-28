namespace ContentTours.Contracts.Authorization;

/// <summary>
/// Feature string constants owned by the ContentTours bounded context.
/// Add one constant per aggregate/resource exposed through endpoints.
/// Other modules MUST NOT add constants here.
/// </summary>
public static class ContentToursFeatures
{
    public const string Tour = nameof(Tour);
}
