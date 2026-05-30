namespace Security.Presentation.Endpoints.User.Models;

public sealed record AddUserClaimRequest(string ClaimType, string ClaimValue);
