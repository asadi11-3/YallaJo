namespace Auth.Application.Commands.SendActivationEmail;

/// <summary>
/// Result payload for <see cref="SendActivationEmailCommand"/>. Carries a
/// human-readable message that legacy façades can surface directly.
/// </summary>
public sealed record SendActivationEmailResult(string Message);
