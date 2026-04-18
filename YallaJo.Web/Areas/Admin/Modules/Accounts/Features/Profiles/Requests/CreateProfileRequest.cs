namespace YallaJo.Web.Areas.Admin.Modules.Accounts.Features.Profiles.Requests;

public sealed record CreateProfileRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl);
