namespace YallaJo.Web.Areas.Business.Models.Hours;

public sealed class BusinessHoursItemResponse
{
    public Guid Id { get; set; }
    public string DayOfWeek { get; set; } = "";
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public bool IsClosed { get; set; }
}

public sealed record SetHoursApiRequest(IReadOnlyList<HoursEntryApiRequest> Hours);

public sealed record HoursEntryApiRequest(int DayOfWeek, string? OpenTime, string? CloseTime, bool IsClosed);
