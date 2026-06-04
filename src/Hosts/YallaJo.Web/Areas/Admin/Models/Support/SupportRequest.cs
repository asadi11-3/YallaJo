namespace YallaJo.Web.Areas.Admin.Models.Support;

public sealed class SupportFilterRequest
{
    public string? Status { get; set; }
    public string? Category { get; set; }
    public Guid? Cursor { get; set; }
    public int PageSize { get; set; } = 20;
}
