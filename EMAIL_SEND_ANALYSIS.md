# Email Send Implementation Analysis - YallaJoJo Repository

## EXECUTIVE SUMMARY

**Single Email Service Implementation:** `GmailEmailService` (System.Net.Mail.SmtpClient)
**Total SendMailAsync Call Sites:** 1 (in GmailEmailService.cs)
**Total SendAsync Call Sites:** 5 (wrapper method calls across application)
**Mail Library:** System.Net.Mail (built-in .NET)

---

## 1. EMAIL SERVICE IMPLEMENTATION

### File: `Auth.Infrastructure/Services/GmailEmailService.cs`

**Lines 1-37 (Complete Implementation)**

```csharp
using System.Net;
using System.Net.Mail;
using Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Services;
internal sealed class GmailEmailService(
    IOptions<GmailOptions> options,
    ILogger<GmailEmailService> logger) : IEmailService
{
    private readonly GmailOptions _opts = options.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(_opts.SenderEmail, _opts.AppPassword),
            EnableSsl = true
        };

        var message = new MailMessage(
            from: _opts.SenderEmail,
            to: to,
            subject: subject,
            body: body)
        {
            IsBodyHtml = false
        };

        logger.LogInformation("Sending email to {To} with subject '{Subject}'", to, subject);

        await client.SendMailAsync(message, ct);

        logger.LogInformation("Email sent successfully to {To}", to);
    }
}
```

**Key Details:**
- **Line 16:** SmtpClient instantiation with Gmail SMTP server
- **Line 18:** NetworkCredential using `_opts.SenderEmail` and `_opts.AppPassword`
- **Line 19:** EnableSsl = true (required for Gmail port 587)
- **Lines 22-29:** MailMessage construction with from/to/subject/body
- **Line 28:** IsBodyHtml = false (plain text only)
- **Line 33:** **CRITICAL: `await client.SendMailAsync(message, ct);`** - THE FAILURE POINT

---

## 2. CONFIGURATION

### File: `Auth.Infrastructure/Services/GmailOptions.cs`

```csharp
namespace Auth.Infrastructure.Services;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    public string SenderEmail { get; init; } = string.Empty;
    public string AppPassword { get; init; } = string.Empty;
}
```

### Configuration Values (appsettings.json)

**File:** `YallaJo.Api/appsettings.json` (Lines 41-44)

```json
"Gmail": {
    "SenderEmail": "muhmooud2003@gmail.com",
    "AppPassword": "etmq nvyb tteo hklx"
}
```

**SUSPICIOUS VALUES DETECTED:**
1. **AppPassword contains spaces:** `"etmq nvyb tteo hklx"` 
   - Gmail App Passwords should NOT contain spaces
   - This is a 16-character password with 3 spaces (likely formatted for readability)
   - **LIKELY FAILURE CAUSE:** SmtpClient may not handle spaces in credentials correctly
   - **ACTION:** Remove spaces from AppPassword: `"etmqnvybtteohhklx"` or verify actual password format

2. **SenderEmail:** `muhmooud2003@gmail.com` (appears valid)

---

## 3. DEPENDENCY INJECTION

### File: `Auth.Infrastructure/DependencyInjection.cs` (Lines 56-57)

```csharp
services.Configure<GmailOptions>(configuration.GetSection(GmailOptions.SectionName));
services.AddScoped<IEmailService, GmailEmailService>();
```

**Registration:** Scoped lifetime, configured from "Gmail" section

---

## 4. INTERFACE CONTRACT

### File: `Auth.Application/Interfaces/IEmailService.cs`

```csharp
namespace Auth.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default);
}
```

---

## 5. SENDMAILASYNC CALL SITES (5 Total)

### Call Site 1: UserCreatedIntegrationEventHandler

**File:** `Auth.Application/EventHandlers/UserCreatedIntegrationEventHandler.cs`
**Lines:** 66-70

```csharp
await emailService.SendAsync(
    evt.Email,
    "YallaJo — Verify Your Email",
    $"Your verification code is: {plainOtp}\n\nThis code expires in {OtpExpiryMinutes} minutes.",
    ct);
```

**Context:**
- **Recipient:** `evt.Email` (from UserCreatedIntegrationEvent)
- **Subject:** "YallaJo — Verify Your Email"
- **Body:** Plain text with OTP code
- **Error Handling:** Lines 79-89 - Catches exceptions, marks OTP as used, logs error, re-throws
- **Surrounding Code:** Lines 64-90 (try-catch block)

**Potential Issues:**
- Email address from event may not be validated/normalized
- No check if email is null/empty before sending

---

### Call Site 2: ResendOtpCommandHandler

**File:** `Auth.Application/Commands/ResendOtp/ResendOtpCommandHandler.cs`
**Lines:** 80-84

```csharp
await emailService.SendAsync(
    normalizedEmail,
    subject,
    $"Your verification code is: {plainOtp}. It expires in 10 minutes.",
    cancellationToken);
```

**Context:**
- **Recipient:** `normalizedEmail` (trimmed and lowercased at line 26)
- **Subject:** Dynamic - "YallaJo — Reset Your Password" or "YallaJo — Verify Your Email" (lines 74-76)
- **Body:** Plain text with OTP code
- **Error Handling:** Lines 88-101 - Catches exceptions, marks OTP as used, returns failure result
- **Surrounding Code:** Lines 78-101 (try-catch block)

**Potential Issues:**
- Email normalization is good (trimmed + lowercase)
- Error handling returns Result instead of re-throwing

---

### Call Site 3: ResendInviteCommandHandler

**File:** `Auth.Application/Commands/ResendInvite/ResendInviteCommandHandler.cs`
**Lines:** 77-83

```csharp
await emailService.SendAsync(
    normalizedEmail,
    "YallaJo — Your invite link",
    $"Your YallaJo invite has been refreshed.\n" +
    $"Click the link below to set your password and activate your account:\n\n{link}\n\n" +
    $"This link expires in {InviteConstants.ExpiryMinutes / 60 / 24} days.",
    ct);
```

**Context:**
- **Recipient:** `normalizedEmail` (trimmed and lowercased at line 32)
- **Subject:** "YallaJo — Your invite link"
- **Body:** Multi-line plain text with invite link
- **Error Handling:** NO TRY-CATCH - Exception will propagate to caller
- **Surrounding Code:** Lines 77-83 (no error handling)

**Potential Issues:**
- **CRITICAL:** No error handling - if SendAsync fails, entire command fails
- Email normalization is good
- Link construction at line 75 - could be malformed

---

### Call Site 4: InviteUserCommandHandler

**File:** `Auth.Application/Commands/InviteUser/InviteUserCommandHandler.cs`
**Lines:** 95-101

```csharp
await emailService.SendAsync(
    normalizedEmail,
    "YallaJo — You're invited",
    $"Hello {request.FirstName},\n\nYou have been invited to join YallaJo.\n" +
    $"Click the link below to set your password and activate your account:\n\n{link}\n\n" +
    $"This link expires in {InviteConstants.ExpiryMinutes / 60 / 24} days.",
    ct);
```

**Context:**
- **Recipient:** `normalizedEmail` (trimmed and lowercased at line 38)
- **Subject:** "YallaJo — You're invited"
- **Body:** Multi-line plain text with personalization and invite link
- **Error Handling:** NO TRY-CATCH - Exception will propagate to caller
- **Surrounding Code:** Lines 95-101 (no error handling)

**Potential Issues:**
- **CRITICAL:** No error handling - if SendAsync fails, entire command fails
- Email normalization is good
- FirstName could contain special characters that break email body formatting

---

### Call Site 5: ForgotPasswordCommandHandler

**File:** `Auth.Application/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs`
**Lines:** 44-48

```csharp
await emailService.SendAsync(
    normalizedEmail,
    "YallaJo — Reset Your Password",
    $"Your password reset code is: {plainOtp}. It expires in 10 minutes.",
    ct);
```

**Context:**
- **Recipient:** `normalizedEmail` (trimmed and lowercased at line 25)
- **Subject:** "YallaJo — Reset Your Password"
- **Body:** Plain text with OTP code
- **Error Handling:** NO TRY-CATCH - Exception will propagate to caller
- **Surrounding Code:** Lines 44-48 (no error handling)

**Potential Issues:**
- **CRITICAL:** No error handling
