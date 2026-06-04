namespace YallaJo.Web.Areas.Admin.Models.Payments;

public sealed class PaymentsFilterRequest
{
    public string? Status { get; set; }
    public string? Type { get; set; }
    public Guid? Cursor { get; set; }
    public int PageSize { get; set; } = 25;
}
