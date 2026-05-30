namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;

public sealed class ExternalLoginOutcome
{
    public bool IsSuccess { get; private init; }
    public bool IsNotLinked { get; private init; }
    public string? Error { get; private init; }

    public static ExternalLoginOutcome Ok() => new() { IsSuccess = true };

    public static ExternalLoginOutcome NotLinked() =>
        new()
        {
            IsNotLinked = true,
            Error = "We couldn't sign you in with this provider. Please try again, or sign in with email and password.",
        };

    public static ExternalLoginOutcome Fail(string error) => new() { Error = error };
}
