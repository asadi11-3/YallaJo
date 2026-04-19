namespace YallaJo.Web.Infrastructure.Authentication.SignIn;

public interface IWebSignInService
{
    Task SignInAsync(Guid userId, string accessToken, string refreshToken, DateTime refreshTokenExpiresAt);
    Task SignOutAsync();
}
