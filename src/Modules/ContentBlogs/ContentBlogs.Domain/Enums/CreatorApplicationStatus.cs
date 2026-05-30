namespace ContentBlogs.Domain.Enums;

/// <summary>
/// Lifecycle state of a <see cref="Entities.Creators.CreatorApplication"/>.
/// Wave 7 – Content Creator Module.
/// </summary>
public enum CreatorApplicationStatus : byte
{
    /// <summary>Initial state – applicant has not yet submitted.</summary>
    Draft = 0,

    /// <summary>Submitted and awaiting admin review.</summary>
    Pending = 1,

    /// <summary>Admin approved the application; a <see cref="Entities.Creators.CreatorProfile"/> is created.</summary>
    Approved = 2,

    /// <summary>Admin rejected the application.</summary>
    Rejected = 3,

    /// <summary>Admin requested additional information from the applicant.</summary>
    MoreInfoNeeded = 4
}
