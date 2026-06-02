namespace YallaJo.Web.Areas.Auth.Models.Register;

public sealed class RegisterResponse
{
    public Guid   UserId  { get; init; }
    public string Message { get; init; } = string.Empty;
}
