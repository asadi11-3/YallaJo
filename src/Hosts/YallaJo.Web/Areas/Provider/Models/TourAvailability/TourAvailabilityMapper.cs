using System.Globalization;

namespace YallaJo.Web.Areas.Provider.Models.TourAvailability;

public static class TourAvailabilityMapper
{
    // ── List ────────────────────────────────────────────────────────────────────────

    public static TourAvailabilityIndexVm ToIndexVm(
        Guid tourId, string tourName, string tourStatusLabel,
        IReadOnlyList<ManageAvailabilitySlotResponse> slots) => new()
    {
        TourId          = tourId,
        TourName        = tourName,
        TourStatusLabel = tourStatusLabel,
        Slots           = slots
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(ToRowVm)
            .ToList(),
    };

    private static AvailabilitySlotRowVm ToRowVm(ManageAvailabilitySlotResponse s) => new()
    {
        Id             = s.Id,
        Date           = s.Date,
        DateLabel      = s.Date.ToString("ddd, dd MMM yyyy", CultureInfo.InvariantCulture),
        StartTime      = s.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        EndTime        = s.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
        MaxCapacity    = s.MaxCapacity,
        AvailableCount = s.AvailableCount,
        IsActive       = s.IsActive,
        RowVersion     = s.RowVersion,
    };

    // ── Form ─────────────────────────────────────────────────────────────────────────

    public static AvailabilitySlotFormVm ToCreateVm(Guid tourId, string tourName) => new()
    {
        TourId      = tourId,
        TourName    = tourName,
        IsEdit      = false,
        Date        = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        StartTime   = new TimeOnly(9, 0),
        EndTime     = new TimeOnly(12, 0),
        MaxCapacity = 1,
    };

    public static AvailabilitySlotFormVm ToEditVm(Guid tourId, string tourName, ManageAvailabilitySlotResponse s) => new()
    {
        TourId      = tourId,
        SlotId      = s.Id,
        TourName    = tourName,
        IsEdit      = true,
        Date        = s.Date,
        StartTime   = s.StartTime,
        EndTime     = s.EndTime,
        MaxCapacity = s.MaxCapacity,
        RowVersion  = s.RowVersion,
    };

    // ── Form → API request ───────────────────────────────────────────────────────────

    public static CreateAvailabilitySlotApiRequest ToCreateRequest(AvailabilitySlotFormVm vm) => new(
        TourId:      vm.TourId,
        Date:        vm.Date ?? default,
        StartTime:   vm.StartTime ?? default,
        EndTime:     vm.EndTime ?? default,
        MaxCapacity: vm.MaxCapacity);

    public static UpdateAvailabilitySlotApiRequest ToUpdateRequest(AvailabilitySlotFormVm vm) => new(
        MaxCapacity: vm.MaxCapacity,
        RowVersion:  vm.RowVersion ?? string.Empty);
}
