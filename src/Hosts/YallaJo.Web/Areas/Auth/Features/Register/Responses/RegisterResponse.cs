namespace YallaJo.Web.Areas.Auth.Features.Register.Responses;

public sealed class RegisterResponse
{
    public Guid   UserId  { get; init; }
    public string Message { get; init; } = string.Empty;
}
