namespace Social.Domain.Enums;

/// <summary>Indicates who or what triggered the deletion of a review.</summary>
public enum ReviewDeletionSource : byte
{
    User   = 0,
    Admin  = 1,
    System = 2,
}
