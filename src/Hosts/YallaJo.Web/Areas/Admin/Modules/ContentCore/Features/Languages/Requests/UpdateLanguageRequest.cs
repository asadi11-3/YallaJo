namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Requests;

public sealed record UpdateLanguageRequest(string Name, string NativeName, bool IsRtl, bool IsActive);
