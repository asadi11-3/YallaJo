using YallaJo.SharedKernel.Application.Authorization;

namespace ContentTours.Contracts.Authorization;

/// <summary>
/// Permission catalog for the ContentTours bounded context.
/// Registered in ContentTours.Infrastructure DI — discovered automatically by PermissionSeeder.
/// </summary>
public sealed class ContentToursPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "ContentTours";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── Tour ─────────────────────────────────────────────────────────────
        new(ContentToursFeatures.Tour, AppAction.Read,      PermissionGroup.ContentManagement, "View tours"),
        new(ContentToursFeatures.Tour, AppAction.Create,    PermissionGroup.ContentManagement, "Create a tour"),
        new(ContentToursFeatures.Tour, AppAction.Update,    PermissionGroup.ContentManagement, "Update tour details"),
        new(ContentToursFeatures.Tour, AppAction.Delete,    PermissionGroup.ContentManagement, "Delete a tour"),
        new(ContentToursFeatures.Tour, AppAction.Submit,    PermissionGroup.ContentManagement, "Submit a draft tour for review"),
        new(ContentToursFeatures.Tour, AppAction.Approve,   PermissionGroup.ContentManagement, "Approve a submitted tour"),
        new(ContentToursFeatures.Tour, AppAction.Reject,    PermissionGroup.ContentManagement, "Reject a submitted tour"),
        new(ContentToursFeatures.Tour, AppAction.Suspend,   PermissionGroup.ContentManagement, "Suspend an approved tour"),
        new(ContentToursFeatures.Tour, AppAction.Reinstate, PermissionGroup.ContentManagement, "Reinstate a suspended tour"),
        new(ContentToursFeatures.Tour, AppAction.ReadOwn,   PermissionGroup.ContentManagement, "View own tours (provider dashboard)"),
        new(ContentToursFeatures.Tour, AppAction.ReadAny,   PermissionGroup.ContentManagement, "View any tour (admin)"),
        new(ContentToursFeatures.Tour, AppAction.Feature,   PermissionGroup.ContentManagement, "Feature or unfeature a tour (admin curation)"),
        new(ContentToursFeatures.Tour, AppAction.Archive,   PermissionGroup.ContentManagement, "Archive a tour (provider)"),

        new(ContentToursFeatures.Package, AppAction.Read,   PermissionGroup.ContentManagement, "View tour packages"),
        new(ContentToursFeatures.Package, AppAction.Create, PermissionGroup.ContentManagement, "Create a tour package"),
        new(ContentToursFeatures.Package, AppAction.Update, PermissionGroup.ContentManagement, "Update a tour package or add inclusions"),
        new(ContentToursFeatures.Package, AppAction.Delete, PermissionGroup.ContentManagement, "Soft-delete a tour package"),

        // ── TourGuide (assign / unassign guides on a tour) ────────────────────
        new(ContentToursFeatures.TourGuide, AppAction.Update, PermissionGroup.ContentManagement, "Assign or unassign guides on a tour"),

        // ── TourPricingTier ──────────────────────────────────────────────────
        new(ContentToursFeatures.TourPricingTier, AppAction.Create, PermissionGroup.ContentManagement, "Create a pricing tier on a tour"),
        new(ContentToursFeatures.TourPricingTier, AppAction.Update, PermissionGroup.ContentManagement, "Update a pricing tier on a tour"),
        new(ContentToursFeatures.TourPricingTier, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a pricing tier on a tour"),

        // ── TourSchedule ─────────────────────────────────────────────────────
        new(ContentToursFeatures.TourSchedule, AppAction.Create, PermissionGroup.ContentManagement, "Create schedule(s) for a tour"),
        new(ContentToursFeatures.TourSchedule, AppAction.Update, PermissionGroup.ContentManagement, "Update a tour schedule"),
        new(ContentToursFeatures.TourSchedule, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a tour schedule"),

        // ── TourWaypoint ─────────────────────────────────────────────────────
        new(ContentToursFeatures.TourWaypoint, AppAction.Create, PermissionGroup.ContentManagement, "Add a waypoint to a tour"),
        new(ContentToursFeatures.TourWaypoint, AppAction.Update, PermissionGroup.ContentManagement, "Reorder waypoints on a tour"),
        new(ContentToursFeatures.TourWaypoint, AppAction.Delete, PermissionGroup.ContentManagement, "Remove a waypoint from a tour"),

        // ── TourChildrenInfo (children-related fields block on a tour) ───────
        new(ContentToursFeatures.TourChildrenInfo, AppAction.Update, PermissionGroup.ContentManagement, "Update the children-info block on a tour"),

        // ── GuideApplication ─────────────────────────────────────────────────
        new(ContentToursFeatures.GuideApplication, AppAction.Create,  PermissionGroup.ContentManagement, "Apply to run an existing tour (guide)"),
        new(ContentToursFeatures.GuideApplication, AppAction.Read,    PermissionGroup.ContentManagement, "View guide applications for a tour"),
        new(ContentToursFeatures.GuideApplication, AppAction.Approve, PermissionGroup.ContentManagement, "Approve a guide application (admin)"),
        new(ContentToursFeatures.GuideApplication, AppAction.Reject,  PermissionGroup.ContentManagement, "Reject a guide application (admin)"),

        // ── TourProposal ──────────────────────────────────────────────────────
        new(ContentToursFeatures.TourProposal, AppAction.Create,  PermissionGroup.ContentManagement, "Submit a new tour proposal (guide)"),
        new(ContentToursFeatures.TourProposal, AppAction.Read,    PermissionGroup.ContentManagement, "View tour proposals"),
        new(ContentToursFeatures.TourProposal, AppAction.Submit,  PermissionGroup.ContentManagement, "Submit a draft proposal for review (guide)"),
        new(ContentToursFeatures.TourProposal, AppAction.Approve, PermissionGroup.ContentManagement, "Approve a tour proposal — creates the tour (admin)"),
        new(ContentToursFeatures.TourProposal, AppAction.Reject,  PermissionGroup.ContentManagement, "Reject a tour proposal (admin)"),

        // ── GuideOffering ─────────────────────────────────────────────────────
        new(ContentToursFeatures.GuideOffering, AppAction.Read,      PermissionGroup.ContentManagement, "View guide offerings for a tour"),
        new(ContentToursFeatures.GuideOffering, AppAction.Create,    PermissionGroup.ContentManagement, "Create guide schedules, pricing tiers, or enable private tours"),
        new(ContentToursFeatures.GuideOffering, AppAction.Update,    PermissionGroup.ContentManagement, "Update guide schedules or pricing tiers"),
        new(ContentToursFeatures.GuideOffering, AppAction.Delete,    PermissionGroup.ContentManagement, "Delete guide schedules, pricing tiers, or remove offerings"),
        new(ContentToursFeatures.GuideOffering, AppAction.Suspend,   PermissionGroup.ContentManagement, "Suspend a guide offering (admin)"),
        new(ContentToursFeatures.GuideOffering, AppAction.Reinstate, PermissionGroup.ContentManagement, "Reinstate a guide offering (admin)"),

        // ── TourGuideProfile (admin management) ──────────────────────────────
        new(ContentToursFeatures.TourGuideProfile, AppAction.Read,     PermissionGroup.ContentManagement, "View tour guide profiles"),
        new(ContentToursFeatures.TourGuideProfile, AppAction.Update,   PermissionGroup.ContentManagement, "Update own guide profile (self-service)"),
        new(ContentToursFeatures.TourGuideProfile, AppAction.Suspend,  PermissionGroup.ContentManagement, "Suspend a tour guide (admin)"),
        new(ContentToursFeatures.TourGuideProfile, AppAction.Reinstate,PermissionGroup.ContentManagement, "Reinstate a tour guide (admin)"),
        new(ContentToursFeatures.TourGuideProfile, AppAction.Delete,   PermissionGroup.ContentManagement, "Self-deactivate guide profile"),
    ];
}
