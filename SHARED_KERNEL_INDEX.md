# YallaJoJo Shared Kernel & Web Infrastructure - Complete Index

## 📚 Documentation Files

### 1. **SHARED_KERNEL_DEEP_DIVE.md** (192 lines)
Comprehensive technical documentation covering:
- Shared Kernel architecture (Domain, Application, Infrastructure, Presentation)
- Web project configuration (MVC + BFF pattern)
- API project configuration (REST + JWT)
- Auth module integration
- Design patterns and security considerations
- Quick reference guides

### 2. **EXPLORATION_SUMMARY.txt** (193 lines)
Executive summary with:
- Key findings organized by component
- Authentication flow diagrams
- Design patterns overview
- Security considerations
- Dependency injection summary
- Quick reference for common tasks

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    YallaJo Modular Monolith                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │         YallaJo.SharedKernel (Core Foundation)           │  │
│  ├──────────────────────────────────────────────────────────┤  │
│  │ Domain: BaseEntity, Result<T>, Error, IRepository        │  │
│  │ Application: ICommand, IQuery, ValidationBehavior        │  │
│  │ Infrastructure: UnitOfWork, EfRepository, DependencyInjection
│  │ Presentation: ResultExtensions (Result → IResult)        │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              YallaJo.Web (MVC + BFF)                     │  │
│  ├──────────────────────────────────────────────────────────┤  │
│  │ Authentication: CookieAuthenticationDefaults             │  │
│  │ JwtAuthHandler: Transparent token refresh                │  │
│  │ ApiClient: Typed HTTP client (through JwtAuthHandler)    │  │
│  │ WebSignInService: Creates encrypted cookie              │  │
│  │ Auth Features: Login, Logout, Sessions, Devices, etc.   │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              YallaJo.Api (REST + JWT)                    │  │
│  ├──────────────────────────────────────────────────────────┤  │
│  │ Authentication: JwtBearerDefaults                        │  │
│  │ CurrentUser: Reads from JWT claims                       │  │
│  │ RequestContext: Extracts HTTP metadata                   │  │
│  │ Authorization: Role-based + Permission-based             │  │
│  │ Exception Handlers: Global error handling                │  │
│  │ Middleware: Localization, Rate limiting, etc.            │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Auth Module (Bridge)                        │  │
│  ├──────────────────────────────────────────────────────────┤  │
│  │ Application: LoginCommand, RefreshTokenCommand, etc.     │  │
│  │ Presentation: Minimal APIs (/api/v1/auth/*)              │  │
│  │ Web Features: Controllers, Facades, ApiClients           │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │         Other Modules (Accounts, Content, etc.)          │  │
│  ├──────────────────────────────────────────────────────────┤  │
│  │ All modules follow the same patterns and inherit from    │  │
│  │ the Shared Kernel, ensuring consistency across the       │  │
│  │ codebase.                                                │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔐 Authentication Flow

### Login Flow
```
User Form → Web Controller → Facade → ApiClient → API Endpoint
                                                      ↓
                                            LoginCommand Handler
                                            (verify, create device/session/token)
                                                      ↓
                                            Return LoginResult
                                                      ↓
                                            WebSignInService
                                            (create encrypted cookie)
                                                      ↓
                                            Redirect to /auth/sessions
```

### Authenticated Request Flow
```
Browser (with cookie) → JwtAuthHandler
                            ↓
                    Check token expiry
                    ├─ Valid: Attach Bearer token
                    └─ Expiring: Refresh token first
                            ↓
                    API Endpoint (with Authorization header)
                            ↓
                    JWT validation → ClaimsPrincipal
                            ↓
                    ICurrentUser + IRequestContext
                            ↓
                    Handler execution
                            ↓
                    Response
```

---

## 📋 Key Components

### Shared Kernel

| Component | Purpose | Key Classes |
|-----------|---------|-------------|
| **Domain** | Core abstractions | BaseEntity, Result<T>, Error, IRepository |
| **Application** | CQRS, validation | ICommand, IQuery, ValidationBehavior, ICurrentUser |
| **Infrastructure** | EF Core, MediatR | UnitOfWork, EfRepository, DependencyInjection |
| **Presentation** | HTTP mapping | ResultExtensions |

### Web Project

| Component | Purpose | Key Classes |
|-----------|---------|-------------|
| **Authentication** | Cookie-based auth | CookieAuthenticationDefaults |
| **Token Handler** | Transparent refresh | JwtAuthHandler |
| **HTTP Client** | API communication | ApiClient, ApiResult |
| **Sign-In Service** | Cookie creation | WebSignInService |
| **Auth Features** | Login, logout, etc. | LoginFacade, LoginController |

### API Project

| Component | Purpose | Key Classes |
|-----------|---------|-------------|
| **Authentication** | JWT validation | JwtBearerDefaults |
| **Context Services** | User/request data | CurrentUser, RequestContext |
| **Authorization** | Role/permission checks | PermissionAuthorizationHandler |
| **Exception Handling** | Error responses | GlobalExceptionHandler |
| **Middleware** | Request processing | RequestLocalizationMiddleware |

### Auth Module

| Component | Purpose | Key Classes |
|-----------|---------|-------------|
| **Application** | Business logic | LoginCommand, RefreshTokenCommand |
| **Presentation** | API endpoints | CredentialEndpoints, SessionEndpoints |
| **Web Features** | MVC controllers | LoginController, LoginFacade |

---

## 🔑 Key Abstractions

### Result Types (Railway-Oriented Programming)

```csharp
// Instead of throwing exceptions:
public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken ct)
{
    if (userData is null)
        return Result<LoginResult>.Failure(
            Error.Unauthorized("Invalid email or password."),
            Outcome.Unauthorized);
    
    return Result<LoginResult>.Success(new LoginResult(...));
}

// Caller pattern-matches:
var result = await handler.Handle(command, ct);
if (result.IsSuccess)
    return result.ToApiResult();  // 200 OK
else
    return result.ToApiResult();  // RFC 7807 Problem
```

### CQRS Pattern

```csharp
// Command (write)
public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResult>;
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResult> { ... }

// Query (read
