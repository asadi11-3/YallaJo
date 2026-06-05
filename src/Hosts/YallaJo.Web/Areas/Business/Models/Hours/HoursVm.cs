using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Business.Models.Hours;

public sealed class HoursVm
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public string Status { get; set; } = "";
    public List<DayHoursFormVm> Days { get; set; } = [];
}

public sealed class DayHoursFormVm
{
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    public string DayName { get; set; } = "";

    [Display(Name = "Closed")]
    public bool IsClosed { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Opens")]
    public string? OpenTime { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Closes")]
    public string? CloseTime { get; set; }
}
