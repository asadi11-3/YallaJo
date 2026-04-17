# YallaJoJo Project Architecture Analysis

## 1. Overall Project Structure

### Monorepo Organization
The project uses a **modular monorepo** architecture with clean separation of concerns:

```
YallaJoJo/
├── YallaJo.Api/                    # Backend API (ASP.NET Core)
├── YallaJo.Web/                    # Frontend MVC (ASP.NET Core MVC)
├── YallaJo.SharedKernel.*/         # Cross-cutting abstractions
├── [Module].Application/           # Business logic (MediatR commands/queries)
├── [Module].Contracts/             # DTOs and contracts
├── [Module].Domain/                # Domain entities and aggregates
├── [Module].Infrastructure/        # Data access, external services
├── [Module].Presentation/          # API endpoints (for API modules)
└── tests/                          # Test projects
```

### Modules (Areas)
The system is organized into **vertical slices** (modules):
- **Auth** - Authentication, sessions, devices, refresh tokens
- **Accounts** - User profiles and account management
- **Security** - Roles, claims, permissions, audit logs
- **Booking** - Reservations, trips, payments
- **Content*** - Blogs, Places, Tours, SEO, Core
- **Analytics, Finance, Messaging, Social, Tracking**

Each module follows the same layered pattern:
- **Application** - Commands/Queries (MediatR)
- **Domain** - Entities, aggregates, repositories
- **Infrastructure** - DbContext, repositories, services
- **Presentation** - API endpoints (for API modules)

---

## 2. Auth Area Structure (Web Frontend)

### Current State
The Auth Area in `YallaJo.Web/Areas/Auth/` is **scaffolded but empty**:

```
YallaJo.Web/Areas/Auth/
├── Features/
│   ├── Login/
│   │   ├── LoginController.cs          (empty)
│   │   ├── LoginFacade.cs              (empty)
│   │   ├── LoginApiClient.cs           (empty)
│   │   ├── ViewModels/LoginVm.cs       (empty)
│   │   ├── Requests/LoginRequest.cs    (empty)
│   │   ├── Responses/LoginResponse.cs  (empty)
│   │   ├── Validators/LoginVmValidator.cs
│   │   ├── Mappers/LoginMapper.cs
│   │   └── Views/Index.cshtml
│   ├── Register/
│   ├── ForgotPassword/
│   ├── ResetPassword/
│   ├── VerifyEmail/
│   ├── Logout/
│   ├── LogoutAll/
│   ├── Sessions/
│   ├── Devices/
│   └── ExternalProviders/
```

**All classes are empty stubs** - ready for implementation.

---

## 3. Existing Auth Infrastructure

### Backend API (YallaJo.Api)

#### Authentication Setup (Program.cs)
```csharp
// JWT Bearer authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Authorization policies
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("Owner", p => p.RequireRole(AppRoles.Owner));
    opts.AddPolicy("SuperAdmin", p => p.RequireRole(AppRoles.Owner, AppRoles.SuperAdmin));
    opts.AddPolicy("Admin", p => p.RequireRole(AppRoles.Owner, AppRoles.SuperAdmin, AppRoles.Admin));
});

// Custom permission-based authorization
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
```

#### JWT Configuration (appsettings.Development.json)
```json
{
  "Jwt": {
    "Issuer": "YallaJo.Api",
    "Audience": "YallaJo.Clients",
    "Key": "oubogewubfpwbvpwb6513534y836qpoiwhfnmmtnrypisbvspv5H",
    "AccessTokenMinutes": 60
  }
}
```

### Frontend Web (YallaJo.Web)

#### Authentication Setup (Program.cs)
```csharp
// Cookie-based authentication (MVC frontend)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = "YallaJo.Web";
    });

// HttpClient for API communication (BFF pattern)
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<JwtAuthHandler>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddHttpMessageHandler<JwtAuthHandler>();
```

#### API Configuration (appsettings.json)
```json
{
  "ApiBaseUrl": "https://localhost:57065"
}
```

**Note:** `ApiClient` and `JwtAuthHandler` are referenced but **not yet implemented** in the codebase.

---

## 4. Backend Auth Module (API)

### Auth.Presentation - Endpoints

Located in `Auth.Presentation/Endpoints/Credential/`:

#### Endpoints Defined
- **POST /auth/verify-email** - Verify email with OTP
- **POST /auth/login** - Login with email/password → returns access + refresh tokens
- **POST /auth/refresh** - Refresh access token
- **POST /auth/forgot-password** - Request password reset
- **POST /auth/reset-password** - Reset password with OTP
- **POST /auth/resend-otp** - Resend OTP code

#### Models
```csharp
// Request
public sealed record LoginRequest(string Email, string Password);

// Response
public sealed record LoginResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
```

### Auth.Application - Commands & Queries

#### Commands (MediatR)
- `LoginCommand` → `LoginCommandHandler` → `LoginResult`
- `RefreshTokenCommand`
- `LogoutCommand`
- `LogoutAllCommand`
- `VerifyEmailCommand`
- `ForgotPasswordCommand`
- `ResetPasswordCommand`
- `ResendOtpCommand`
- `TrustDeviceCommand`
- `RevokeSessionCommand`
- `LinkExternalProviderCommand`
- `UnlinkExternalProviderCommand`

#### Queries
- `ListSessionsQuery`

### Auth.Domain - Entities & Aggregates

#### Core Entities
```csharp
// Session - represents an active user session
public sealed class Session : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid DeviceId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? IpAddress { get; private set; }
}

// RefreshToken - stores hashed refresh tokens
public sealed class RefreshToken : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
}

// Device - tracks user devices
public sealed class Device : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string DeviceToken { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceName { get; private set; }
    public bool IsTrusted { get; private set; }
    public DateTime? TrustedAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }
}

// Otp - one-time passwords for email verification
public sealed class Otp : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string Code { get; private set; }
    public string Purpose { get; private set; }
    publi
