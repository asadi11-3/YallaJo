namespace ContentTours.Presentation.Endpoints.TourPackage.Models;

/// <summary>
///     Admin reject-tour-package request body (WS-5a Phase 3 G3a).
/// </summary>
internal sealed record RejectTourPackageRequest(string Reason);
