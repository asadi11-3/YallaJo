using Auth.Application.Commands.ForgotPassword;
using Auth.Application.Commands.Login;
using Auth.Application.Commands.Logout;
using Auth.Application.Commands.LogoutAll;
using Auth.Application.Commands.RefreshToken;
using Auth.Application.Commands.ResendOtp;
using Auth.Application.Commands.ResetPassword;
using Auth.Application.Commands.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Presentation;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/verify-email", async (VerifyEmailRequest request, ISender sender) =>
        {
            var result = await sender.Send(new VerifyEmailCommand(request.Email, request.OtpCode));
            return ToApiResult(result);
        })
        .WithName("VerifyEmail")
        .Produces<VerifyEmailResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Verify email with OTP code — returns access + refresh tokens")
        .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, ISender sender) =>
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password));
            return ToApiResult(result);
        })
        .WithName("Login")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Login with email and password — returns access + refresh tokens")
        .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenRequest request, ISender sender) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken));
            return ToApiResult(result);
        })
        .WithName("RefreshToken")
        .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Refresh access token using a valid refresh token")
        .AllowAnonymous();

        group.MapPost("/logout", async (LogoutRequest request, ISender sender) =>
        {
            var result = await sender.Send(new LogoutCommand(request.RefreshToken));
            return ToApiResult(result);
        })
        .WithName("Logout")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Logout — revokes the refresh token and its session")
        .RequireAuthorization();

        group.MapPost("/logout-all", async (ISender sender) =>
        {
            var result = await sender.Send(new LogoutAllCommand());
            return ToApiResult(result);
        })
        .WithName("LogoutAll")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Logout all sessions — revokes all refresh tokens and sessions for the current user")
        .RequireAuthorization();

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ForgotPasswordCommand(request.Email));
            return ToApiResult(result);
        })
        .WithName("ForgotPassword")
        .Produces<ForgotPasswordResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Request a password reset code — sends OTP to email if account exists")
        .AllowAnonymous();

        group.MapPost("/reset-password", async (ResetPasswordRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ResetPasswordCommand(
                request.Email,
                request.OtpCode,
                request.NewPassword,
                request.ConfirmNewPassword));
            return ToApiResult(result);
        })
        .WithName("ResetPassword")
        .Produces<ResetPasswordResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Reset password using email and OTP code")
        .AllowAnonymous();

        group.MapPost("/resend-otp", async (ResendOtpRequest request, ISender sender) =>
        {
            var result = await sender.Send(new ResendOtpCommand(request.Email, request.Purpose));
            return ToApiResult(result);
        })
        .WithName("ResendOtp")
        .Produces<ResendOtpResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Resend OTP code for email verification or password reset")
        .AllowAnonymous();

        return endpoints;
    }

    // ── Result → IResult mapping ──────────────────────────────────────────

    private static IResult ToApiResult<T>(Result<T> result) =>
        result.IsSuccess
            ? result.Outcome == Outcome.Created
                ? Results.Created((string?)null, result.Value)
                : Results.Ok(result.Value)
            : ToProblem(result.Outcome, result.Errors);

    private static IResult ToApiResult(Result result) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblem(result.Outcome, result.Errors);

    private static IResult ToProblem(Outcome outcome, IReadOnlyList<Error> errors)
    {
        var first = errors.Count > 0 ? errors[0] : null;
        return Results.Problem(
            statusCode: (int)outcome,
            title: first?.Code,
            detail: first?.Message);
    }
}

// ── Request/Response DTOs ────────────────────────────────────────────────

public sealed record VerifyEmailRequest(string Email, string OtpCode);
public sealed record VerifyEmailResponse(Guid UserId, string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(Guid UserId, string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
public sealed record LogoutRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string OtpCode, string NewPassword, string ConfirmNewPassword);
public sealed record ResendOtpRequest(string Email, string Purpose);
