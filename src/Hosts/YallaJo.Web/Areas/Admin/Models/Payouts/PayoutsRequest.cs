namespace YallaJo.Web.Areas.Admin.Models.Payouts;

public sealed class PayoutsFilterRequest
{
    public Guid? Cursor { get; set; }
    public int PageSize { get; set; } = 25;
}
