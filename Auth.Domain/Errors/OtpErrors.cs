using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Domain.Errors;

public static class OtpErrors
{
    public static readonly Error NotFound = new(
        "NotFound.Otp",
        "No pending verification code found.");
}
