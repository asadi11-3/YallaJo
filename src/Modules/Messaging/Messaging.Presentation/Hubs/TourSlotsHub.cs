using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Messaging.Presentation.Hubs;

/// <summary>
/// Public, anonymous SignalR hub for live tour availability-slot capacity (UI-PERF S2 / CAL3 / RT1).
/// This is the ONE public-page hub exception: anonymous visitors on the tour-detail page
/// (/tours/{slug}) subscribe ONLY to the <c>tour:{tourId}</c> group to receive
/// <c>SlotCapacityChanged</c> events. It carries NO JWT and exposes NO method that can join
/// the privileged <c>user:</c> / <c>provider:</c> / <c>admin</c> groups (those live on the
/// authenticated <see cref="NotificationHub"/>). Group names are derived server-side from the
/// supplied tourId — the client can never pass a raw group name (SignalR groups are not a
/// security boundary, so we never expose a generic JoinGroup).
/// </summary>
[AllowAnonymous]
public sealed class TourSlotsHub : Hub
{
    /// <summary>Tracks the single tour group this connection has joined (one tour per connection).</summary>
    private const string JoinedTourKey = "joined-tour-group";

    /// <summary>
    /// On connect, if the page supplied <c>?tourId=</c> on the negotiate/connect URL, auto-join
    /// that tour group so updates flow immediately without a round-trip.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var raw = Context.GetHttpContext()?.Request.Query["tourId"].ToString();
        if (Guid.TryParse(raw, out var tourId) && tourId != Guid.Empty)
        {
            await JoinTour(tourId).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Joins the <c>tour:{tourId}</c> group. The group name is ALWAYS built server-side from the
    /// typed <paramref name="tourId"/>; a client cannot reach user/provider/admin groups.
    /// One tour group per connection: re-joining a different tour leaves the previous one.
    /// </summary>
    public async Task JoinTour(Guid tourId)
    {
        if (tourId == Guid.Empty)
        {
            return;
        }

        var groupName = TourGroup(tourId);

        if (Context.Items.TryGetValue(JoinedTourKey, out var existing)
            && existing is string current
            && current != groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, current).ConfigureAwait(false);
        }

        Context.Items[JoinedTourKey] = groupName;
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
    }

    /// <summary>Leaves the tour group (e.g. SPA-style navigation away from the tour page).</summary>
    public async Task LeaveTour(Guid tourId)
    {
        if (tourId == Guid.Empty)
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TourGroup(tourId)).ConfigureAwait(false);
        Context.Items.Remove(JoinedTourKey);
    }

    /// <summary>The canonical group name for a tour. Used by the broadcast handler too.</summary>
    public static string TourGroup(Guid tourId) => $"tour:{tourId}";
}
