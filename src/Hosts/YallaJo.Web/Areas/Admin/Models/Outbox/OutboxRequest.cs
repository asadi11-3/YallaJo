namespace YallaJo.Web.Areas.Admin.Models.Outbox;

public sealed class OutboxFilterRequest
{
    public string? Module { get; set; }
    public int Limit { get; set; } = 50;
}
