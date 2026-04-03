namespace Security.Presentation.Endpoints.User.Models;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);
