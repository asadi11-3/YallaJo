namespace YallaJo.Web.Areas.Auth.Models.Login;

public sealed class LoginResult
{
    public bool IsSuccess { get; private init; }
    public string? Error { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static LoginResult Ok() => new() { IsSuccess = true };
    public static LoginResult Fail(string error) => new() { IsSuccess = false, Error = error };
    public static LoginResult Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { IsSuccess = false, ValidationErrors = errors };
}
