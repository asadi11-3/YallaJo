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
        new(ContentToursFeatures.Tour, AppAction.Approve,   PermissionGroup.ContentManagement, "Approve a submitted tour"),
        new(ContentToursFeatures.Tour, AppAction.Reject,    PermissionGroup.ContentManagement, "Reject a submitted tour"),
        new(ContentToursFeatures.Tour, AppAction.Suspend,   PermissionGroup.ContentManagement, "Suspend an approved tour"),
        new(ContentToursFeatures.Tour, AppAction.Reinstate, PermissionGroup.ContentManagement, "Reinstate a suspended tour"),
        new(ContentToursFeatures.Tour, AppAction.ReadOwn,   PermissionGroup.ContentManagement, "View own tours (provider dashboard)"),
        new(ContentToursFeatures.Tour, AppAction.ReadAny,   PermissionGroup.ContentManagement, "View any tour (admin)"),
        new(ContentToursFeatures.Tour, AppAction.Feature,   PermissionGroup.ContentManagement, "Feature or unfeature a tour (admin curation)"),
    ];
}
