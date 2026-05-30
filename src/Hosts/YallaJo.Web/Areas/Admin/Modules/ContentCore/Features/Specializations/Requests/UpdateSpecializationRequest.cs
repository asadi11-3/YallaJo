namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Specializations.Requests;

public sealed record UpdateSpecializationRequest(string Name, string? Description, string? Icon, bool? IsActive);
