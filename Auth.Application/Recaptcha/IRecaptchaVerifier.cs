using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Recaptcha;

public interface IRecaptchaVerifier
{
    Task<Result> VerifyAsync(
        string token,
        string expectedAction,
        string? remoteIp = null,
        CancellationToken ct = default);
}
