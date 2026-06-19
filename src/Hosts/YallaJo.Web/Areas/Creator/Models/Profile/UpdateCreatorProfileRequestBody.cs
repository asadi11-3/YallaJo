namespace YallaJo.Web.Areas.Creator.Models.Profile;


public sealed record UpdateCreatorProfileRequestBody(
    string DisplayName,
    string? Bio,
    string? NewSlug);
