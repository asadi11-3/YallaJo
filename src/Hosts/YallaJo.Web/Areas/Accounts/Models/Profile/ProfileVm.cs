using YallaJo.Web.Areas.Accounts.Models.Promo;

namespace YallaJo.Web.Areas.Accounts.Models.Profile;

public sealed class ProfileVm
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName  { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? AvatarUrl   { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string? AddressLine { get; init; }
    public string Email { get; init; } = string.Empty;

    public UpdateProfileVm Update { get; init; } = new();
    public UpdateAvatarVm UpdateAvatar { get; init; } = new();

    public IReadOnlyList<PromoBlockVm> Promos { get; init; } = [];

    // Profile-completion checks (server is source of truth; logic mirrors the prior view-side calc).
    public bool NameComplete => !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);
    public bool EmailComplete => !string.IsNullOrWhiteSpace(Email);
    public bool MobileComplete => !string.IsNullOrWhiteSpace(PhoneNumber);
    public bool DobComplete => DateOfBirth is not null;
    public bool AddressComplete =>
        !string.IsNullOrWhiteSpace(AddressLine)
        || !string.IsNullOrWhiteSpace(City)
        || !string.IsNullOrWhiteSpace(Country);

    public int CompletionPercent
    {
        get
        {
            var done =
                (NameComplete ? 1 : 0)
                + (EmailComplete ? 1 : 0)
                + (MobileComplete ? 1 : 0)
                + (DobComplete ? 1 : 0)
                + (AddressComplete ? 1 : 0);
            return (int)System.Math.Round(100.0 * done / 5);
        }
    }
}
