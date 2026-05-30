namespace ContentTours.Application.Queries.ChildrenInfo.GetTourChildrenInfo;

public sealed record ChildrenInfoDto(
    bool AllowsChildren,
    int? MinChildAge,
    int? MaxChildAge,
    IReadOnlyList<string> ChildFacilities);
