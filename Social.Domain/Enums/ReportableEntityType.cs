namespace Social.Domain.Enums;

/// <summary>Entity types that can be reported for policy violations.</summary>
public enum ReportableEntityType : byte
{
    Review   = 0,
    Tour     = 1,
    Place    = 2,
    Business = 3,
    Blog     = 4,
}
