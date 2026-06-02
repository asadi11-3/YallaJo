namespace YallaJo.Web.Areas.Accounts.Models.Profile;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    int? Gender,
    string? Country,
    string? City,
    string? AddressLine);
