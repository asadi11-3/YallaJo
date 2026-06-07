namespace YallaJo.Web.Areas.Provider.Models.TourWaypoints;

public static class TourWaypointsMapper
{
    public static TourWaypointsIndexVm ToIndexVm(
        Guid tourId, string tourName, string tourStatusLabel,
        IReadOnlyList<TourWaypointResponse> waypoints) => new()
    {
        TourId          = tourId,
        TourName        = tourName,
        TourStatusLabel = tourStatusLabel,
        Waypoints       = waypoints
            .OrderBy(w => w.SortOrder)
            .Select(ToRowVm)
            .ToList(),
    };

    private static TourWaypointRowVm ToRowVm(TourWaypointResponse w) => new()
    {
        Id                  = w.Id,
        Name                = w.Name,
        Description         = w.Description,
        Latitude            = w.Latitude,
        Longitude           = w.Longitude,
        SortOrder           = w.SortOrder,
        IsMeetingPoint      = w.IsMeetingPoint,
        StopDurationMinutes = w.StopDurationMinutes,
    };

    public static TourWaypointFormVm ToCreateVm(Guid tourId, string tourName) => new()
    {
        TourId   = tourId,
        TourName = tourName,
    };

    public static TourWaypointFormVm ToEditVm(Guid tourId, string tourName, TourWaypointResponse w) => new()
    {
        TourId              = tourId,
        WaypointId          = w.Id,
        TourName            = tourName,
        Name                = w.Name,
        Description         = w.Description,
        Latitude            = w.Latitude,
        Longitude           = w.Longitude,
        IsMeetingPoint      = w.IsMeetingPoint,
        StopDurationMinutes = w.StopDurationMinutes,
    };

    public static CreateTourWaypointApiRequest ToCreateRequest(TourWaypointFormVm vm) => new(
        Name:                vm.Name.Trim(),
        Description:         string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
        Latitude:            vm.Latitude,
        Longitude:           vm.Longitude,
        IsMeetingPoint:      vm.IsMeetingPoint,
        StopDurationMinutes: vm.StopDurationMinutes);

    public static UpdateTourWaypointApiRequest ToUpdateRequest(TourWaypointFormVm vm) => new(
        Name:                vm.Name.Trim(),
        Description:         string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
        Latitude:            vm.Latitude,
        Longitude:           vm.Longitude,
        IsMeetingPoint:      vm.IsMeetingPoint,
        StopDurationMinutes: vm.StopDurationMinutes);
}
