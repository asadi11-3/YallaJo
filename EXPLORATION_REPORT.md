# YallaJo.Web Exploration Report

## Executive Summary
The YallaJo.Web project is an ASP.NET Core 9.0 MVC frontend that acts as a Backend-for-Frontend (BFF) pattern implementation. It uses cookie-based authentication for the web UI and communicates with the YallaJo.Api backend via JWT-authenticated HTTP calls. The project structure is well-organized with Areas, but most feature classes are currently empty stubs awaiting implementation.

---

## 1. PROJECT STRUCTURE

### Root Directory Contents
```
YallaJo.Web/
├── Areas/                          # Feature areas (Auth, Accounts, Admin, Content)
├── Controllers/                    # Empty - no root controllers
├── Infrastructure/                 # Auth, API, Authorization, Constants, etc.
├── Models/                         # Empty
├── Shared/                         # Components, Extensions, Filters, Layout, etc.
├── Views/                          # Razor views
├── Program.cs                      # Main entry point
├── appsettings.json               # Configuration
├── appsettings.Development.json   # Dev configuration
└── YallaJo.Web.csproj            # Project file
```

### Areas Structure
```
Areas/
├── Auth/                           # Authentication features
│   └── Features/
│       ├── Devices/
│       ├── ExternalProviders/
│       ├── ForgotPassword/
│       ├── Login/
│       ├── Logout/
│       ├── LogoutAll/
│       ├── Register/
│       ├── ResetPassword/
│       ├── Sessions/
│       └── VerifyEmail/
├── Accounts/                       # User account management
│   └── Features/
│       └── Profile/
├── Admin/                          # Admin dashboard
│   └── Modules/
│       ├── Booking/
│       ├── ContentCore/
│       ├── ContentPlaces/
│       ├── Dashboard/
│       └── Security/
└── Content/                        # Public content browsing
    └── Features/
        ├── Categories/
        ├── Places/
        ├── Search/
        └── Tags/
```

### Infrastructure Directory
```
Infrastructure/
├── Api/
│   ├── Contracts/                  # Empty
│   ├── Core/                       # Empty
│   ├── Handlers/                   # Empty (should contain JwtAuthHandler)
│   └── Serialization/              # Empty
├── Authentication/
│   ├── Claims/                     # Empty
│   ├── Cookie/                     # Empty
│   ├── SignIn/                     # Empty
│   └── Tokens/                     # Empty
├── Authorization/                  # Empty
├── Constants/                      # Empty
├── DependencyInjection/            # Empty
├── Navigation/                     # Empty
└── State/                          # Empty
```

---

## 2. PROGRAM.CS ANALYSIS

### Key Configuration Points

#### Authentication Setup (Lines 6-19)
```csharp
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
```

**Status**: ✅ Configured
- Uses cookie-based authentication (appropriate for MVC frontend)
- 8-hour expiration with sliding window
- HttpOnly and Strict SameSite for security
- Cookie name: "YallaJo.Web"

#### HTTP Client Setup (Lines 23-34)
```csharp
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

**Status**: ⚠️ INCOMPLETE - Classes don't exist yet
- References `JwtAuthHandler` - **NOT IMPLEMENTED**
- References `ApiClient` - **NOT IMPLEMENTED**
- Both need to be created in Infrastructure/Api

#### Middleware Pipeline (Lines 41-62)
```csharp
app.UseExceptionHandler("/error");
app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");
```

**Status**: ✅ Configured
- Proper middleware ordering
- Areas routing configured
- Default route points to Auth/Login

---

## 3. APPSETTINGS ANALYSIS

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiBaseUrl": "https://localhost:57065"
}
```

**Status**: ✅ Configured
- API base URL points to backend API
- Logging configured appropriately

### appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Trace"
    }
  },
  "AllowedHosts": "*",
  "ApiBaseUrl": "https://localhost:57065"
}
```

**Status**: ✅ Configured
- Development logging set to Trace level

---

## 4. NUGET PACKAGES

### Current Dependencies (YallaJo.Web.csproj)
```xml
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.7.0" />
```

**Status**: ⚠️ MINIMAL
- Only JWT token library included
- **Missing**: No HTTP client libraries, no validation libraries, no mapping libraries

**Recommended Additions**:
- `FluentValidation` - For request validation
- `AutoMapper` - For DTO/ViewModel mapping
- `MediatR` - For command/query pattern (if following API pattern)
- `Serilog` - For structured logging
- `Polly` - For HTTP resilience

---

## 5. EXISTING PATTERNS & ARCHITECTURE

### Feature Structure Pattern
Each feature follows this structure:
```
Feature/
├── {Feature}ApiClient.cs          # HTTP client for API calls
├── {Feature}Controller.cs         # MVC controller
├── {Feature}Facade.cs             # Business logic orchestration
├── Mappers/
│   └── {Feature}Mapper.cs         # DTO ↔ ViewModel mapping
├── Requests/
│   └── {Request}.cs               # API request DTOs
├── Responses/
│   └── {Response}.cs              # API response DTOs
├── Validators/
│   └── {Feature}VmValidator.cs    # ViewModel validation
└── ViewModels/
    └── {Feature}Vm.cs             # MVC ViewModels
```

**Current Status**: All classes are empty stubs (6 lines each)

### Example: Profile Feature
- **ProfileApiClient.cs** - Empty, should handle API calls to /api/v1/accounts/profile
- **ProfileController.cs** - Has basic Index() action returning View()
- **ProfileFacade.cs** - Empty, should orchestrate ProfileApiClient calls
- **ProfileMapper.cs** - Empty, should map API responses to ViewModels
- **UpdateProfileRequest.cs** - Empty DTO
- **UpdateProfileVm.cs** - Empty ViewModel
- **UpdateProfileVmValidator.cs** - Empty validator

---

## 6. BACKEND API PATTERNS (Reference)

### Auth API Endpoints (Auth.Presentation)
```
POST /api/v1/auth/login              → LoginResponse (AccessToken, RefreshToken)
POST /api/v1/auth/refresh            → RefreshTokenResponse
POST /api/v1/auth/verify-email       → VerifyEmailResponse
POST /api/v1/auth/forgot-password    → ForgotPasswordResult
POST /api/v1/auth/reset-password     → ResetPasswordResult
POST /api/v1/auth/resend-otp         → ResendOtpResult
```

### Token Structure (JwtTokenService)
```csharp
public sealed record TokenData(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    IReadOnlyList<(string Type, string Value)> AdditionalClaims,
    Guid? SessionId = null);
```

**JWT Claims**:
- `sub` - User ID (Guid)
- `email` - User email
- `jti` - JWT ID (unique identifier)
- `iat` - Issued at (Unix timestamp)
- `role` - User roles (multiple)
- `permission` - User permissions (multiple)
- `sid` - Session ID (optional)

**Token Expiration**:
- Access Toke
