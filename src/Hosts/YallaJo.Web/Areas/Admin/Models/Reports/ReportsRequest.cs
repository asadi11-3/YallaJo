namespace YallaJo.Web.Areas.Admin.Models.Reports;

public sealed class ReportsFilterRequest
{
    public Guid? AfterCursor { get; set; }
    public int PageSize { get; set; } = 20;
}
