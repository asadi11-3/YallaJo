namespace YallaJo.Web.Areas.Admin.Models.Specializations;

public sealed record UpdateSpecializationRequest(string Name, string? Description, string? Icon, bool? IsActive);
