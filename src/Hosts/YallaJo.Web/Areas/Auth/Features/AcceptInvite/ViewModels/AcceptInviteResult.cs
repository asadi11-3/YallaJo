namespace YallaJo.Web.Areas.Auth.Features.AcceptInvite.ViewModels;

public sealed class AcceptInviteResult
{
    public bool IsSuccess { get; private init; }
    public bool IsExpired { get; private init; }
    public bool IsAlreadyCompleted { get; private init; }
    public string? Error { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static AcceptInviteResult Ok() => new() { IsSuccess = true };
    public static AcceptInviteResult Expired(string message) => new() { IsExpired = true, Error = message };
    public static AcceptInviteResult AlreadyCompleted(string message) => new() { IsAlreadyCompleted = true, Error = message };
    public static AcceptInviteResult Fail(string error) => new() { Error = error };
    public static AcceptInviteResult Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { ValidationErrors = errors };
}
