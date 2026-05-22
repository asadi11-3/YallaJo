namespace ContentBlogs.Domain.Enums;

/// <summary>
/// The nature of a creator's relationship to a disclosed entity.
/// Wave 8 – Disclosure enforcement.
/// </summary>
public enum DisclosureRelationKind : byte
{
    /// <summary>Creator owns the listed provider / business.</summary>
    OwnedListing = 0,

    /// <summary>Creator received payment or goods in exchange for the mention.</summary>
    PaidPartnership = 1,

    /// <summary>Creator has a family or personal tie to the entity.</summary>
    FamilyTie = 2,

    /// <summary>Creator earns commission via an affiliate link.</summary>
    AffiliateLink = 3
}
