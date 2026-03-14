namespace YallaJo.Web.Areas.Admin.Models;

public sealed record AdminSectionPageViewModel(
    string Title,
    string Description,
    string Badge,
    string IconClass,
    string AccentClass,
    string[] Highlights);
