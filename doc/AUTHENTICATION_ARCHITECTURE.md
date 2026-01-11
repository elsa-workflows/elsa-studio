# Elsa Studio Authentication Architecture

This document provides an overview of the authentication architecture in Elsa Studio, including how different authentication providers integrate with the framework.

## Overview

Elsa Studio supports multiple authentication providers through a flexible, extensible architecture. The system is designed to:

1. Support multiple authentication mechanisms (OIDC, OAuth2, JWT, etc.)
2. Work across different Blazor hosting models (Server and WebAssembly)
3. Provide automatic token management and refresh
4. Integrate seamlessly with backend API calls and SignalR connections

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                     Elsa Studio Application                      │
│  (Workflows, Dashboard, etc.)                                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Elsa.Studio.Core                              │
│  • ISingleFlightCoordinator - Prevents concurrent token refresh  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
           ┌───────────────┴───────────────┐
           ▼                               ▼
┌──────────────────────┐      ┌──────────────────────────┐
│  OpenIdConnect       │      │  ElsaAuth                │
│  Provider            │      │  Provider                │
│  ────────────────    │      │  ──────────────────────  │
│  • ITokenProvider    │      │  • IAuthenticationProvider│
│  • OidcOptions       │      │  • JWT tokens            │
│  • Server & WASM     │      │  • Server & WASM         │
│  • Auto token refresh│      │  • Auto token refresh    │
└──────────────────────┘      └──────────────────────────┘
```

## OpenID Connect Provider (Blazor Server)

### Architecture

The OIDC Blazor Server implementation follows the standard ASP.NET Core pattern with automatic token refresh:

```
┌─────────────────────────────────────────────────────────────────┐
│                    HTTP Request                                  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│              Cookie Authentication Middleware                    │
│  • Validates cookie on every request                            │
│  • Triggers OnValidatePrincipal event                           │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    AuthCookieEvents                              │
│  • Checks if access_token is expiring (within 2 min skew)       │
│  • If expiring: calls TokenRefreshService                       │
│  • Updates tokens in cookie (context.ShouldRenew = true)        │
│  • If refresh fails: rejects principal → re-authentication      │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   TokenRefreshService                            │
│  • Reads OpenIdConnectOptions (client ID, secret)               │
│  • Discovers token endpoint from OIDC metadata                  │
│  • Performs OAuth2 refresh_token grant                          │
│  • Returns TokenRefreshResult (access_token, expires_at)        │
│  • HTTP client has Polly retry policy (configurable)            │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `TokenRefreshService` | Core OAuth2 refresh token grant logic. Shared between session refresh and backend API token acquisition. |
| `AuthCookieEvents` | Cookie authentication events that automatically refresh tokens via `OnValidatePrincipal`. Standard ASP.NET Core pattern. |
| `ServerTokenProvider` | Implements `ITokenProvider`. Returns cookie's access token, or acquires scope-specific tokens for backend API calls. |
| `OidcOptions` | Configuration: Authority, ClientId, Scopes, BackendApiScopes, etc. |
| `TokenRefreshResult` | Simple result record: Success, AccessToken, RefreshToken, ExpiresAt. |

### Token Flow

**Session Token Refresh (automatic):**
```
HTTP Request → Cookie Middleware → AuthCookieEvents.OnValidatePrincipal
    → Check expires_at (2 min skew)
    → TokenRefreshService.RefreshTokenAsync(refreshToken)
    → Update cookie tokens → Continue request
```

**Backend API Token (on-demand):**
```
ServerTokenProvider.GetAccessTokenAsync()
    → If no BackendApiScopes: return cookie's access_token
    → If BackendApiScopes configured:
        → Check in-memory cache
        → TokenRefreshService.RefreshTokenAsync(refreshToken, scopes)
        → Cache result → Return access_token
```

### Configuration

```csharp
// Program.cs
builder.Services.AddOidcAuthentication(options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret"; // Optional for confidential clients
    options.AuthenticationScopes = ["openid", "profile", "offline_access"];
    
    // Optional: Different scopes for backend API (multi-audience scenarios)
    options.BackendApiScopes = ["api://backend-api/.default"];
});

// Custom retry policy (optional)
builder.Services.AddOidcAuthentication(
    options => { /* ... */ },
    configureRetryPolicy: () => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1)));

app.UseAuthentication();
app.UseAuthorization();
```

### Default Retry Policy

The `TokenRefreshService` HTTP client has a configurable Polly retry policy:

- **Default**: 3 retries with exponential backoff (1s, 2s, 4s)
- **Handles**: Transient HTTP errors (5xx, 408) and 429 Too Many Requests
- **Customizable**: Pass `configureRetryPolicy` to `AddOidcAuthentication()`

## OpenID Connect Provider (Blazor WebAssembly)

Uses Microsoft's built-in `Microsoft.AspNetCore.Components.WebAssembly.Authentication`:

- Tokens managed by browser-based authentication framework
- Accessed via `IAccessTokenProvider`
- Automatic token refresh before expiry
- Secure token storage in browser

## ElsaAuth Provider

For Elsa Identity (username/password authentication against Elsa backend).

### Architecture

The ElsaAuth provider uses JWT tokens stored in browser storage (WASM) or server-side session (Server), with automatic token refresh:

```
┌─────────────────────────────────────────────────────────────────┐
│                 API Call / SignalR Connection                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                  JwtAuthenticationProvider                       │
│  • Implements IAuthenticationProvider                           │
│  • Reads access token from IJwtAccessor                         │
│  • Checks if token is expiring (within 2 min skew)              │
│  • If expiring: calls IRefreshTokenService (single-flight)      │
│  • If refresh fails: clears all tokens → unauthenticated        │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   IRefreshTokenService                           │
│  • Calls Elsa backend's refresh endpoint                        │
│  • Returns IsAuthenticated + new tokens                         │
│  • Tokens stored via IJwtAccessor                               │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `JwtAuthenticationProvider` | Implements `IAuthenticationProvider`. Gets access tokens with automatic refresh before expiry. |
| `IRefreshTokenService` | Calls Elsa backend to refresh tokens. |
| `IJwtAccessor` | Reads/writes JWT tokens (LocalStorage for WASM, session for Server). |
| `IJwtParser` | Parses JWT to extract expiry claim. |

### Token Flow

```csharp
public class JwtAuthenticationProvider : IAuthenticationProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var accessToken = await jwtAccessor.ReadTokenAsync("accessToken");
        
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        if (!IsExpiredOrNearExpiry(accessToken))
            return accessToken;

        // Single-flight refresh via ISingleFlightCoordinator
        var refreshResponse = await refreshCoordinator.RunAsync(
            refreshTokenService.RefreshTokenAsync, cancellationToken);

        if (!refreshResponse.IsAuthenticated)
        {
            // Clear all tokens on refresh failure
            await jwtAccessor.ClearTokenAsync("accessToken");
            await jwtAccessor.ClearTokenAsync("refreshToken");
            await jwtAccessor.ClearTokenAsync("idToken");
            return null;
        }

        return await jwtAccessor.ReadTokenAsync("accessToken");
    }
}
```

### Configuration

```csharp
// Blazor Server
builder.Services.AddElsaAuth();
builder.Services.AddElsaAuthUI();

// Blazor WASM
builder.Services.AddElsaAuth();
builder.Services.AddElsaAuthUI();
```

## Integration Points

### 1. API Calls

The `AuthenticatingApiHttpMessageHandler` automatically adds tokens to API requests:

```csharp
public class AuthenticatingApiHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(...)
    {
        var token = await tokenProvider.GetAccessTokenAsync();
        request.Headers.Authorization = new("Bearer", token);
        // ... handle 401 with token refresh
    }
}
```

### 2. SignalR Connections

SignalR connections are authenticated via `IHttpConnectionOptionsConfigurator`, which is defined in `Elsa.Studio.Authentication.Abstractions` and implemented by each authentication provider.

**Interface** (`Elsa.Studio.Authentication.Abstractions`):
```csharp
public interface IHttpConnectionOptionsConfigurator
{
    Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default);
}
```

**OIDC Implementation** (`Elsa.Studio.Authentication.OpenIdConnect`):
```csharp
public class OidcHttpConnectionOptionsConfigurator(ITokenProvider tokenProvider) : IHttpConnectionOptionsConfigurator
{
    public Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default)
    {
        options.AccessTokenProvider = async () => await tokenProvider.GetAccessTokenAsync(cancellationToken);
        return Task.CompletedTask;
    }
}
```

**Usage in WorkflowInstanceObserverFactory** (`Elsa.Studio.Workflows`):
```csharp
public class WorkflowInstanceObserverFactory(
    // ...
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator)
{
    public async Task<IWorkflowInstanceObserver> CreateAsync(WorkflowInstanceObserverContext context)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Delegates to the provider-specific configurator (e.g., OIDC, ElsaAuth)
                httpConnectionOptionsConfigurator.ConfigureAsync(options, cancellationToken).GetAwaiter().GetResult();
            })
            .Build();
        // ...
    }
}
```

This design allows different authentication providers to configure SignalR connections appropriately:
- **OIDC**: Sets `AccessTokenProvider` to return tokens from `ITokenProvider`
- **ElsaAuth**: Could set authorization headers or cookies
- **Custom providers**: Implement their own configuration logic

## Security Considerations

### Blazor Server (OIDC)

- ✅ Tokens stored server-side in authentication cookie properties
- ✅ Cookie: HttpOnly, Secure, SameSite=Lax
- ✅ 8-hour cookie expiration with sliding window
- ✅ Automatic token refresh via `OnValidatePrincipal` (no JavaScript needed)
- ✅ PKCE enabled by default
- ✅ Token refresh failures force re-authentication

### Blazor WebAssembly (OIDC)

- ✅ Tokens managed by Microsoft's authentication framework
- ✅ Automatic token expiry and renewal
- ✅ Access tokens available, refresh tokens hidden from app code

### Blazor Server (ElsaAuth)

- ✅ JWT tokens stored server-side in session
- ✅ Automatic token refresh via `JwtAuthenticationProvider` (2 min skew)
- ✅ Single-flight coordination prevents concurrent refresh requests
- ✅ Tokens cleared on refresh failure

### Blazor WebAssembly (ElsaAuth)

- ⚠️ JWT tokens stored in browser LocalStorage
- ✅ Automatic token refresh via `JwtAuthenticationProvider` (2 min skew)
- ✅ Single-flight coordination prevents concurrent refresh requests
- ✅ Tokens cleared on refresh failure

## File Structure

### Elsa.Studio.Authentication.OpenIdConnect.BlazorServer

```
Services/
├── TokenRefreshService.cs      # Core OAuth2 refresh token logic
├── AuthCookieEvents.cs         # OnValidatePrincipal for auto-refresh
└── ServerTokenProvider.cs      # ITokenProvider implementation

Models/
└── TokenRefreshResult.cs       # Refresh operation result

Components/
└── ChallengeToLogin.razor      # Unauthorized redirect component

Controllers/
└── AuthenticationController.cs # Login/Logout endpoints

Extensions/
└── ServiceCollectionExtensions.cs  # AddOidcAuthentication()
```

### Elsa.Studio.Authentication.OpenIdConnect (Shared)

```
Models/
└── OidcOptions.cs                          # Configuration options

Services/
└── OidcHttpConnectionOptionsConfigurator.cs  # SignalR connection auth config

Contracts/
└── ITokenProvider.cs                       # Token provider interface
```

### Elsa.Studio.Authentication.Abstractions

```
Contracts/
└── IHttpConnectionOptionsConfigurator.cs   # SignalR auth configuration interface

HttpMessageHandlers/
└── AuthenticatingApiHttpMessageHandler.cs  # Adds auth headers to API requests
```

### Elsa.Studio.Authentication.ElsaAuth

```
Services/
├── JwtAuthenticationProvider.cs            # IAuthenticationProvider with auto-refresh
├── ElsaIdentityRefreshTokenService.cs      # Calls Elsa backend refresh endpoint
├── ElsaAuthHttpConnectionOptionsConfigurator.cs  # SignalR connection auth config
├── JwtAccessorBase.cs                      # Base class for token storage
└── AccessTokenAuthenticationStateProvider.cs  # Blazor auth state

Contracts/
├── IAuthenticationProvider.cs              # Token provider interface
├── IRefreshTokenService.cs                 # Token refresh interface
├── IJwtAccessor.cs                         # Token storage interface
└── IJwtParser.cs                           # JWT parsing interface
```

## Troubleshooting

### Common Issues

1. **Token refresh not working**
   - Ensure `offline_access` scope is requested
   - Verify IdP issues refresh tokens
   - Check `SaveTokens = true` (default)

2. **401 errors on API calls**
   - Check token scopes match API requirements
   - For multi-audience: configure `BackendApiScopes`
   - Verify token audience matches API expectation

3. **Refresh failures cause logout**
   - This is expected behavior - ensures security
   - Check IdP configuration for refresh token lifetime
   - Review logs for specific error messages

4. **Azure AD / Entra ID issues**
   - Use tenant-specific authority: `https://login.microsoftonline.com/{tenant}/v2.0`
   - Request `offline_access` for refresh tokens
   - For backend API: use `api://{app-id}/.default` scope

## Resources

- [Elsa.Studio.Authentication.OpenIdConnect README](../src/modules/Elsa.Studio.Authentication.OpenIdConnect/README.md)
- [Microsoft Blazor Authentication Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)
