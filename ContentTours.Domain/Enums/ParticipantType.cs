namespace ContentTours.Domain.Enums;

/// <summary>
/// Typed classification for a pricing tier.
/// "Adult" is the magic required tier — a Pending/Approved tour must always have at least one
/// active Adult tier before submission is allowed.
///
/// Use <see cref="Adult"/> instead of matching the Name string "Adult" case-insensitively,
/// which is fragile against localization, typos, or renames.
/// </summary>
public enum ParticipantType : byte
{
    Adult   = 1,
    Child   = 2,
    Senior  = 3,
    Student = 4,
    Infant  = 5,
    Group   = 6,
    Family  = 7,
    Other   = 255,
}
