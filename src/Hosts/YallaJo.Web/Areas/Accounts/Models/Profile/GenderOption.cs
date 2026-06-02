namespace YallaJo.Web.Areas.Accounts.Models.Profile;

/// <summary>
/// Mirrors Accounts.Domain.Enums.Gender numeric values.
/// The API JSON pipeline serializes enums by numeric value (no string-enum
/// converter), so the Web must post the integer.
/// </summary>
public enum GenderOption
{
    Male   = 0,
    Female = 1,
}
