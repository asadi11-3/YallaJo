namespace YallaJo.Web.Models.Api;

// ── Request DTOs (sent to the API) ─────────────────────────────────────

public sealed record LoginRequest(string Email, string Password);

// ── Response DTOs (received from the API) ──────────────────────────────

public sealed record LoginResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
