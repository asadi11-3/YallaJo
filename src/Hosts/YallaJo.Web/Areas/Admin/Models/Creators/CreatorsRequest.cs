namespace YallaJo.Web.Areas.Admin.Models.Creators;

public sealed class CreatorsFilterRequest
{
    public string? Status { get; set; }
    public Guid? Id { get; set; }
    public int Page { get; set; } = 1;
}
