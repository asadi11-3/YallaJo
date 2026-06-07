using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.Models.Profile;
using YallaJo.Web.Areas.Accounts.Models.Recommendations;
using YallaJo.Web.Areas.Accounts.Models.Wishlist;

namespace YallaJo.Web.Areas.Accounts.Models.Overview;

/// <summary>
/// Assembles the §3.1 <see cref="OverviewVm"/> from the already-mapped per-feature
/// view models. Pure projection: no I/O, no HttpContext — the controller performs the
/// parallel reads (rule API1) and hands the results here.
/// </summary>
public static class OverviewMapper
{
    /// <summary>Max items shown in each landing-page preview rail.</summary>
    public const int PreviewLimit = 3;

    public static OverviewVm Build(
        ProfileVm? profile,
        BookingsVm? bookings,
        WishlistVm? favorites,
        RecommendationsVm? recommendations,
        int unreadCount)
    {
        var upcoming = bookings?.Bookings ?? [];
        var favs = favorites?.Items ?? [];
        var recs = recommendations?.Recommendations ?? [];

        return new OverviewVm
        {
            DisplayName = ResolveName(profile),
            AvatarUrl = profile?.AvatarUrl,
            UnreadCount = unreadCount,

            UpcomingBookingsCount = upcoming.Count,
            FavoritesCount = favs.Count,
            RecommendationsCount = recs.Count,

            UpcomingBookings = bookings is null ? null : upcoming.Take(PreviewLimit).ToList(),
            Favorites = favorites is null ? null : favs.Take(PreviewLimit).ToList(),
            Recommendations = recommendations is null ? null : recs.Take(PreviewLimit).ToList(),

            BookingsFailed = bookings is null,
            FavoritesFailed = favorites is null,
            RecommendationsFailed = recommendations is null,
        };
    }

    private static string ResolveName(ProfileVm? profile)
    {
        if (profile is null) return "there";
        if (!string.IsNullOrWhiteSpace(profile.DisplayName)) return profile.DisplayName!;
        var full = $"{profile.FirstName} {profile.LastName}".Trim();
        return string.IsNullOrWhiteSpace(full) ? "there" : full;
    }
}
