namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed record UpdateLanguageRequest(string Name, string NativeName, bool IsRtl, bool IsActive);
