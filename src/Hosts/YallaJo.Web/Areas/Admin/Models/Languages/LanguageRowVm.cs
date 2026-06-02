namespace YallaJo.Web.Areas.Admin.Models.Languages;

public sealed class LanguageRowVm
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NativeName { get; init; } = string.Empty;
    public bool IsRtl { get; init; }
    public bool IsActive { get; init; }
}
