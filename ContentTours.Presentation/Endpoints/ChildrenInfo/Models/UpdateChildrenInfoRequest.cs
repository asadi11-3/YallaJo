namespace ContentTours.Presentation.Endpoints.ChildrenInfo.Models;

public sealed record UpdateChildrenInfoRequest(
    bool AllowsChildren,
    int? AgeRestriction,
    int? MinChildAge,
    int? MaxChildAge,
    string? ChildFacilities);
