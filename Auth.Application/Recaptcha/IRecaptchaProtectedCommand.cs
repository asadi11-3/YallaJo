namespace Auth.Application.Recaptcha;

public interface IRecaptchaProtectedCommand
{
    string RecaptchaToken { get; }
    string RecaptchaAction { get; }
}
