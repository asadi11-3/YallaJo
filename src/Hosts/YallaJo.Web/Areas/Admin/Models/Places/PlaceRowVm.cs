namespace YallaJo.Web.Areas.Admin.Models.Places;

public sealed class PlaceRowVm
{
    public Guid             Id            { get; init; }
    public string           Name          { get; init; } = string.Empty;
    public string           Slug          { get; init; } = string.Empty;
    public string           PlaceType     { get; init; } = string.Empty;
    public string?          City          { get; init; }
    public string?          Country       { get; init; }
    public decimal          AverageRating { get; init; }
    public int              ReviewCount   { get; init; }
    public bool             IsFeatured    { get; init; }
    public bool             IsVerified    { get; init; }
}
