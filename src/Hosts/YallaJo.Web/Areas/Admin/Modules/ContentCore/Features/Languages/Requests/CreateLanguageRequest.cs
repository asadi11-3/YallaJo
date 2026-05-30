namespace YallaJo.Web.Areas.Admin.Modules.ContentCore.Features.Languages.Requests;

public sealed record CreateLanguageRequest(string Code, string Name, string NativeName, bool IsRtl);
