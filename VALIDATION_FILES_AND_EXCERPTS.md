# Validation Scripts & Partials - Files & Excerpts

## 📌 Overview

This document provides file paths and relevant code excerpts for all validation-related files found in the YallaJoJo repository.

---

## 🔴 CRITICAL: Missing _ValidationScriptsPartial

### Expected Location
```
YallaJo.Web/Views/Shared/_ValidationScriptsPartial.cshtml
```

### Status
❌ **DOES NOT EXIST** - Referenced in 5 views but not created

### Expected Content
```html
@* jQuery Validate + Unobtrusive Validation *@
<script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/jquery-validate@1.19.5/dist/jquery.validate.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/jquery-validation-unobtrusive@4.0.0/jquery.validate.unobtrusive.min.js"></script>
```

---

## ✅ Existing Validation Files

### 1. Backend Validation Infrastructure

#### ValidationBehavior.cs
**Path**: `YallaJo.SharedKernel.Application/Abstractions/Behaviors/ValidationBehavior.cs`

**Purpose**: Automatic validation of all MediatR requests

**Key Code**:
```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var requestName = typeof(TRequest).Name;
        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            logger.LogWarning(
                "Validation failed for {RequestName}: {Errors}",
                requestName,
                string.Join(", ", failures.Select(f => f.ErrorMessage)));

            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

---

#### ValidationExceptionHandler.cs
**Path**: `YallaJo.Api/ExceptionHandlers/ValidationExceptionHandler.cs`

**Purpose**: Converts FluentValidation exceptions to RFC-7807 400 Bad Request

**Key Code**:
```csharp
internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
            return false;

        var errors = validationException.Errors
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title  = "One or more validation errors occurred."
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
```

---

### 2. ViewModels with Validation

#### LoginVm.cs
**Path**: `YallaJo.Web/Areas/Auth/Features/Login/ViewModels/LoginVm.cs`

**Validation Fields**: Email, Password

**Code**:
```csharp
using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.Login.ViewModels;

/// <summary>View model for the login form.</summary>
public sealed class LoginVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Preserved across the POST → redirect so the user lands where they came from.</summary>
    public string? ReturnUrl { get; set; }
}
```

---

#### ForgotPasswordVm.cs
**Path**: `YallaJo.Web/Areas/Auth/Features/ForgotPassword/ViewModels/ForgotPasswordVm.cs`

**Validation Fields**: Email

**Code**:
```csharp
using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.ForgotPassword.ViewModels;

public sealed class ForgotPasswordVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Set after a successful submission to show a confirmation message.</summary>
    public string? SuccessMessage { get; set; }
}
```

---

#### ResetPasswordVm.cs
**Path**: `YallaJo.Web/Areas/Auth/Features/ResetPassword/ViewModels/ResetPasswordVm.cs`

**Validation Fields**: Email, OtpCode, NewPassword, ConfirmNewPassword

**Code**:
```csharp
using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.ResetPassword.ViewModels;

public sealed class ResetPasswordVm
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required.")]
    public string OtpCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
```

---

#### ExternalProvidersVm.cs
**Path**: `YallaJo.Web/Areas/Auth/Features/ExternalProviders/ViewModels/ExternalProvidersVm.cs`

**Validation Fields**: Provider, ProviderUserId, ProviderEmail

**Code**:
```csharp
using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Auth.Features.ExternalProviders.ViewModels;

public sealed class ExternalProvidersVm
{
    [Required(ErrorMessage = "Provider name is required (e.g. Google, Facebook).")]
    public string  Provider       { get; set; } = string.Empty;

    [Required(ErrorMessage = "Provider user ID is required.")]
    public string  ProviderUserId { get; set; } = string.Empty;

    [EmailAddress]
    public string? ProviderEmail  { get; set; }

    public string? Message { get; set; }
}
```

---

### 3. Views with Validation

#### Login View
**Path**: `YallaJo.Web/Areas/Auth/Features/Login/Views/Index.cshtml`

**Validation Display**: Email, Password

**Key Code**:
```html
@model YallaJo.Web.Areas.Auth.Features.Login.ViewModels.LoginVm
@{
    ViewData["Title"] = "Sign In";
    Layout = "~/Views/Shared/_Layout.cshtml";
}

<div class="auth-card">
    <h2>Sign In</h2>
    <div asp-validation-summary="All" class="text-danger mb-3"></div>

    <form asp-area="Auth" asp-controller="Login" asp-action="Index" method="post">
        <input type="hidden" asp-for="ReturnUrl" />
        @Html.AntiForgeryToken()

        <div class="mb-3">
            <label asp-for="Email" class="form-label"></label>
            <input asp-for="Email" class="form-control" autocomplete="email" />
            <span asp-validation-for="Email" class="text-danger"></span>
        </div>

        <div class="mb-3">
            <label asp-for="Password" class="form-label"></label>
            <input asp-for="Password" type="password" class="form-control" autocomplete="current-password" />
            <span asp-validation-for="Password" class="text-danger"></span>
        </div>

        <button type="submit" class="btn btn-primary w-100">Sign In</button>
    </form>

    <div class="mt-3">
        <a asp-area="Auth" asp-contro
