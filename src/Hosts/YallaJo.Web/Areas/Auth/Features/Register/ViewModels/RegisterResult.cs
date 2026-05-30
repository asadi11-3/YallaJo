namespace YallaJo.Web.Areas.Auth.Features.Register.ViewModels;

public sealed class RegisterResult
{
    public bool IsSuccess { get; private init; }
    public string? Email { get; private init; }
    public string? Error { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public static RegisterResult Ok(string email) => new() { IsSuccess = true, Email = email };
    public static RegisterResult Fail(string error) => new() { IsSuccess = false, Error = error };
    public static RegisterResult Invalid(IReadOnlyDictionary<string, string[]> errors)
        => new() { IsSuccess = false, ValidationErrors = errors };
}
