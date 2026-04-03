using Accounts.Domain.Enums;

namespace Accounts.Presentation.Endpoints.Profile.Models;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender? Gender,
    string? Country,
    string? City,
    string? AddressLine);
