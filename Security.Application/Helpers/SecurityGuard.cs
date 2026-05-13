namespace Security.Application.Helpers;

public static class SecurityGuard
{
    public static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
}
