namespace Accounts.Contracts.Abstractions;

/// <summary>
/// Mirror of <c>Accounts.Domain.Enums.ProviderType</c> for cross-module use.
/// Values must stay in sync with the domain enum.
/// </summary>
public enum ProviderTypeValue
{
    TourOperator = 0,
    IndependentGuide = 1,
    HotelResort = 2,
    ActivityCenter = 3,
    Agency = 4,
    BusinessOwner = 5
}

/// <summary>
/// Cross-module contract allowing other modules to read the provider type
/// of a user's approved provider application.
/// </summary>
public interface IProviderTypeReader
{
    /// <summary>
    /// Returns the <see cref="ProviderTypeValue"/> of the user's approved provider application,
    /// or <c>null</c> if the user has no approved application.
    /// </summary>
    Task<ProviderTypeValue?> GetProviderTypeAsync(Guid userId, CancellationToken cancellationToken = default);
}
