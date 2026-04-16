using Auth.Application.Commands.ForgotPassword;
using Auth.Application.Commands.Login;
using Auth.Application.Commands.RefreshToken;
using Auth.Application.Commands.ResendOtp;
using Auth.Application.Commands.ResetPassword;
using Auth.Application.Commands.VerifyEmail;
using Auth.Presentation.Endpoints.Credential.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Presentation;

namespace Auth.Presentation.Endpoints.Credential;

internal static class CredentialEndpoints
{
    internal static void MapCredentialEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/verify-email", async (VerifyEmailRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new VerifyEmailCommand(request.Email, request.OtpCode), ct);
            return result.ToApiResult();
        })
        .WithName("VerifyEmail")
        .Produces<VerifyEmailResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Verify email with OTP code — returns access + refresh tokens")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.OtpPolicy);

        group.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
            return result.ToApiResult();
        })
        .WithName("Login")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Login with email and password — returns access + refresh tokens")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.LoginPolicy);

        group.MapPost("/refresh", async (RefreshTokenRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken), ct);
            return result.ToApiResult();
        })
        .WithName("RefreshToken")
        .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Refresh access token using a valid refresh token")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.RefreshPolicy);

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ForgotPasswordCommand(request.Email), ct);
            return result.ToApiResult();
        })
        .WithName("ForgotPassword")
        .Produces<ForgotPasswordResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Request a password reset code — sends OTP to email if account exists")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.OtpPolicy);

        group.MapPost("/reset-password", async (ResetPasswordRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ResetPasswordCommand(
                request.Email,
                request.OtpCode,
                request.NewPassword,
                request.ConfirmNewPassword), ct);
            return result.ToApiResult();
        })
        .WithName("ResetPassword")
        .Produces<ResetPasswordResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Reset password using email and OTP code")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.OtpPolicy);

        group.MapPost("/resend-otp", async (ResendOtpRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResendOtpCommand(request.Email, request.Purpose), ct);
            return result.ToApiResult();
        })
        .WithName("ResendOtp")
        .Produces<ResendOtpResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Resend OTP code for email verification or password reset")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.OtpPolicy);
    }
}
