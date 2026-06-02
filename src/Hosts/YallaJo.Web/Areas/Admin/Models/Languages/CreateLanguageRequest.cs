namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed record CreateLanguageRequest(string Code, string Name, string NativeName, bool IsRtl);
