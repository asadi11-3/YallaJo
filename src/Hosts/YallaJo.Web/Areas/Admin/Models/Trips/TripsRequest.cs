namespace YallaJo.Web.Areas.Admin.Models.Trips;

/// <summary>Query parameters for the Tour approvals page: optional id lookup plus browse page.</summary>
public sealed class TripsLookupRequest
{
    public Guid? Id { get; set; }
    public int Page { get; set; } = 1;
}
