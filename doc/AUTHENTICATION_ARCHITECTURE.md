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
│  (Workflows, Dashboard, etc. - uses IAuthenticationProviderManager) │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Elsa.Studio.Core                              │
│  • IAuthenticationProvider - Gets tokens for the app             │
│  • IAuthenticationProviderManager - Manages multiple providers   │
│  • TokenNames - Standard token name constants                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│            Elsa.Studio.Authentication.Abstractions               │
│  • ITokenAccessor - Provider-agnostic token access               │
│  • AuthenticationOptions - Base configuration                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
           ┌───────────────┴───────────────┬─────────────────┐
           ▼                               ▼                 ▼
┌──────────────────────┐      ┌──────────────────┐   ┌─────────────┐
│  OIDC Provider       │      │  ElsaAuth        │   │ Future      │
│  • IOidcTokenAccessor│      │  Provider        │   │ Providers   │
│  • OidcOptions       │      │  • JWT tokens    │   │ (JWT, SAML) │
│  • Server & WASM     │      │  • Server & WASM │   │             │
└──────────────────────┘      └──────────────────┘   └─────────────┘
```

## Core Concepts

### 1. Token Flow

```
Application Request
    ↓
IAuthenticationProviderManager.GetAuthenticationTokenAsync()
    ↓
[Iterates through registered IAuthenticationProvider instances]
    ↓
IAuthenticationProvider.GetAccessTokenAsync()
    ↓
ITokenAccessor.GetTokenAsync()
    ↓
[Provider-specific token retrieval]
    ↓
Token returned to application
```

### 2. Provider Registration

Authentication providers are registered in the service collection and used by the application:

```csharp
// Example: Register ElsaAuth for Elsa Identity authentication
services.AddElsaAuth();
services.AddElsaAuthUI();

// OR

// Example: Register OIDC for OpenID Connect authentication
services.AddOidcAuthentication(options => { /* OIDC config */ });

// Multiple providers can be registered if needed, though typically only one is used
// The manager will try each provider until a valid token is found
```

### 3. Hosting Model Differences

#### Blazor Server

**OpenID Connect**:
- Uses ASP.NET Core authentication middleware
- Tokens stored server-side in authentication properties
- Accessed via `HttpContext.GetTokenAsync()`
- Cookie-based session management
- No client-side token exposure

**ElsaAuth**:
- Uses ASP.NET Core authentication
- Tokens stored server-side in session
- JWT-based authentication with Elsa backend
- Cookie-based session management

#### Blazor WebAssembly

**OpenID Connect**:
- Uses `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- Tokens managed by browser-based authentication framework
- Accessed via `IAccessTokenProvider`
- Automatic token refresh before expiry
- Secure token storage in browser

**ElsaAuth**:
- Uses Blazored.LocalStorage for token persistence
- JWT tokens stored in browser local storage
- Manual token refresh handling
- Credentials validated against Elsa Identity backend

## Standard Interfaces

### IAuthenticationProvider (Core)

The main interface used by Elsa Studio applications.

```csharp
public interface IAuthenticationProvider
{
    Task<string?> GetAccessTokenAsync(string tokenName, CancellationToken cancellationToken = default);
}
```

**Purpose**: Provides tokens to the application for API calls and SignalR connections.

### IAuthenticationProviderManager (Core)

Manages multiple authentication providers.

```csharp
public interface IAuthenticationProviderManager
{
    Task<string?> GetAuthenticationTokenAsync(string? tokenName, CancellationToken cancellationToken = default);
}
```

**Purpose**: Iterates through registered providers to find a valid token.

### ITokenAccessor (Abstractions)

Provider-agnostic interface for token retrieval.

```csharp
public interface ITokenAccessor
{
    Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default);
}
```

**Purpose**: Allows authentication providers to implement token access in their own way.

## Token Names

Standard token names are defined in `TokenNames` class:

- `TokenNames.AccessToken` - Access token for API authentication
- `TokenNames.IdToken` - Identity token (may not be available in all providers/hosting models)
- `TokenNames.RefreshToken` - Refresh token (may not be available in all providers/hosting models)

## Authentication Providers

### Current Providers

#### 1. Elsa.Studio.Authentication.ElsaAuth
- **Location**: `src/modules/Elsa.Studio.Authentication.ElsaAuth*`
- **Supports**: Elsa Identity (username/password authentication against Elsa backend)
- **Modules**:
  - `Elsa.Studio.Authentication.ElsaAuth` - Core ElsaAuth functionality
  - `Elsa.Studio.Authentication.ElsaAuth.BlazorServer` - Blazor Server implementation
  - `Elsa.Studio.Authentication.ElsaAuth.BlazorWasm` - Blazor WebAssembly implementation
  - `Elsa.Studio.Authentication.ElsaAuth.UI` - Login UI components (login page, unauthorized redirects)
- **Features**:
  - Username/password authentication
  - JWT token storage and management
  - Automatic token refresh via refresh tokens
  - Works with Elsa.Identity backend module

#### 2. Elsa.Studio.Authentication.OpenIdConnect
- **Location**: `src/modules/Elsa.Studio.Authentication.OpenIdConnect*`
- **Supports**: OpenID Connect
- **Modules**:
  - `Elsa.Studio.Authentication.OpenIdConnect` - Core OIDC functionality
  - `Elsa.Studio.Authentication.OpenIdConnect.BlazorServer` - Blazor Server implementation
  - `Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm` - Blazor WebAssembly implementation
- **Features**:
  - Uses Microsoft's built-in OIDC handlers
  - Automatic token refresh
  - PKCE support
  - Cookie-based auth (Server) or framework-managed (WASM)
  - Silent token refresh on Blazor Server

#### 3. Elsa.Studio.Login (Legacy/Deprecated)
- **Location**: `src/modules/Elsa.Studio.Login`
- **Supports**: OIDC, OAuth2, Elsa Identity
- **Status**: Deprecated - Use `Elsa.Studio.Authentication.ElsaAuth` or `Elsa.Studio.Authentication.OpenIdConnect` instead
- **Note**: Maintained for backward compatibility but new projects should use the new authentication modules

### Future Providers (Examples)

- `Elsa.Studio.Authentication.OAuth2` - Pure OAuth2 without OIDC
- `Elsa.Studio.Authentication.Jwt` - JWT bearer token authentication
- `Elsa.Studio.Authentication.Saml` - SAML authentication
- `Elsa.Studio.Authentication.AzureAD` - Azure AD specific optimizations
- Custom implementations for proprietary auth systems

## Integration Points

### 1. API Calls

The `AuthenticatingApiHttpMessageHandler` automatically adds authentication tokens to API requests:

```csharp
// In authentication modules
public class AuthenticatingApiHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(...)
    {
        var token = await authenticationProviderManager
            .GetAuthenticationTokenAsync(TokenNames.AccessToken);
        request.Headers.Authorization = new("Bearer", token);
        // ... handle 401 with token refresh
    }
}
```

### 2. SignalR Connections

The `WorkflowInstanceObserverFactory` retrieves tokens for SignalR hub connections:

```csharp
var token = await authenticationProviderManager
    .GetAuthenticationTokenAsync(TokenNames.AccessToken, cancellationToken);
    
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl, options => 
    {
        options.AccessTokenProvider = () => Task.FromResult(token);
    })
    .Build();
```

### 3. Authorization State

Blazor's `AuthenticationStateProvider` is used for UI authorization:

```razor
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        <!-- Show protected content -->
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

## Security Considerations

### Server Hosting

**OpenID Connect**:
- ✅ Tokens never exposed to client browser
- ✅ Cookie-based authentication with HTTP-only cookies
- ✅ Secure server-side session management
- ✅ HTTPS-only cookies in production

**ElsaAuth**:
- ✅ Tokens stored server-side in session
- ✅ Cookie-based session management
- ✅ No direct client-side token exposure

### WASM Hosting

**OpenID Connect**:
- ✅ Tokens managed by authentication framework
- ✅ Automatic token expiry and renewal
- ✅ Access tokens available, but refresh tokens hidden
- ✅ Uses standard browser security features

**ElsaAuth**:
- ⚠️ JWT tokens stored in browser local storage
- ✅ Token refresh via refresh tokens
- ⚠️ Tokens accessible via browser dev tools (standard for SPA authentication)
- ✅ Automatic cleanup on logout

### General
- ✅ PKCE enabled by default for OIDC
- ✅ HTTPS required for metadata endpoints
- ✅ Token refresh on 401 responses
- ✅ Secure token storage per hosting model
- ✅ Authorization state managed via `AuthenticationStateProvider`

## Implementation Guide

### Creating a New Authentication Provider

See `src/modules/Elsa.Studio.Authentication.Abstractions/README.md` for detailed guidance.

**Quick Steps**:

1. Create provider-specific options extending `AuthenticationOptions`
2. Implement `ITokenAccessor` for your provider
3. Implement `IAuthenticationProvider` using your token accessor
4. Create hosting-specific implementations if needed (Server vs WASM)
5. Register services in DI container

### Using an Authentication Provider

**ElsaAuth (Elsa Identity) - Blazor Server**:
```csharp
// Register ElsaAuth services
builder.Services.AddElsaAuth();

// Register UI components (login page, unauthorized redirects)
builder.Services.AddElsaAuthUI();

// Middleware
app.UseAuthentication();
app.UseAuthorization();
```

**ElsaAuth (Elsa Identity) - Blazor WASM**:
```csharp
// Register ElsaAuth services
builder.Services.AddElsaAuth();

// Register UI components (login page, unauthorized redirects)
builder.Services.AddElsaAuthUI();
```

**OpenID Connect - Blazor Server**:
```csharp
builder.Services.AddOidcAuthentication(options =>
{
    options.Authority = "https://identity-server.com";
    options.ClientId = "elsa-studio";
    options.ClientSecret = "secret";
    options.Scopes = new[] { "openid", "profile", "elsa_api" };
});

app.UseAuthentication();
app.UseAuthorization();
```

**OpenID Connect - Blazor WASM**:
```csharp
builder.Services.AddElsaOidcAuthentication(options =>
{
    options.Authority = "https://identity-server.com";
    options.ClientId = "elsa-studio-wasm";
    options.Scopes = new[] { "openid", "profile", "elsa_api" };
});
```

## Migration Path

### From Elsa.Studio.Login to New Authentication Modules

The new authentication modules (`Elsa.Studio.Authentication.ElsaAuth` and `Elsa.Studio.Authentication.OpenIdConnect`) are designed to replace the legacy `Elsa.Studio.Login` module:

1. **Phase 1**: Identify your authentication method
   - If using Elsa Identity (username/password): Migrate to `Elsa.Studio.Authentication.ElsaAuth`
   - If using OIDC: Migrate to `Elsa.Studio.Authentication.OpenIdConnect`

2. **Phase 2**: Update service registrations
   - Replace `AddLoginModuleCore()` with appropriate new module registration
   - Replace `UseElsaIdentity()` with `AddElsaAuth()` + `AddElsaAuthUI()`
   - Replace `UseOpenIdConnect()` with `AddOidcAuthentication()` (Server) or `AddElsaOidcAuthentication()` (WASM)

3. **Phase 3**: Test authentication flow
   - Verify login/logout works correctly
   - Verify API calls are authenticated
   - Verify SignalR connections work

4. **Phase 4**: Remove Login module dependency

### Migration Examples

#### From Elsa.Studio.Login (Elsa Identity) to ElsaAuth

**Before (Blazor Server)**:
```csharp
builder.Services.AddLoginModuleCore()
    .UseElsaIdentity();
```

**After (Blazor Server)**:
```csharp
builder.Services.AddElsaAuth();
builder.Services.AddElsaAuthUI();
```

#### From Elsa.Studio.Login (OIDC) to OpenIdConnect

**Before (Blazor Server)**:
```csharp
builder.Services.AddLoginModuleCore()
    .UseOpenIdConnect(options =>
    {
        options.AuthEndpoint = "https://identity-server.com/connect/authorize";
        options.TokenEndpoint = "https://identity-server.com/connect/token";
        options.EndSessionEndpoint = "https://identity-server.com/connect/endsession";
        options.ClientId = "elsa-studio";
        options.ClientSecret = "secret";
        options.Scopes = new[] { "openid", "profile", "elsa_api" };
    });
```

**After (Blazor Server)**:
```csharp
builder.Services.AddOidcAuthentication(options =>
{
    options.Authority = "https://identity-server.com"; // Auto-discovers endpoints
    options.ClientId = "elsa-studio";
    options.ClientSecret = "secret";
    options.Scopes = new[] { "openid", "profile", "elsa_api", "offline_access" };
});

app.UseAuthentication();
app.UseAuthorization();
```

No breaking changes to existing applications - the legacy module continues to work.

## Troubleshooting

### Common Issues

1. **Multiple providers registered, wrong one used**
   - `IAuthenticationProviderManager` returns first valid token
   - Check registration order
   - Ensure only desired provider is registered

2. **Tokens not available in WASM (OpenIdConnect)**
   - Only access tokens directly accessible
   - Refresh/ID tokens managed by framework

3. **401 errors on API calls**
   - Check token scopes match API requirements
   - Verify `AuthenticatingApiHttpMessageHandler` is registered
   - Check identity server returns correct audience
   - For ElsaAuth: Verify Elsa.Identity backend is configured correctly

4. **SignalR connections fail**
   - Ensure `offline_access` scope for refresh tokens (OpenIdConnect)
   - Verify token provider returns valid token
   - Check SignalR hub authentication configuration

5. **ElsaAuth login fails**
   - Verify backend URL is correctly configured
   - Ensure Elsa.Identity module is installed and configured in backend
   - Check username/password credentials are correct

6. **Configuration-based provider selection not working**
   - Verify `Authentication:Provider` setting in `appsettings.json`
   - Supported values: "OpenIdConnect" or "ElsaAuth"
   - Check that the appropriate configuration section exists (`Authentication:OpenIdConnect` for OIDC)

## Resources

- [Elsa.Studio.Authentication.Abstractions README](../src/modules/Elsa.Studio.Authentication.Abstractions/README.md)
- [Elsa.Studio.Authentication.OpenIdConnect README](../src/modules/Elsa.Studio.Authentication.OpenIdConnect/README.md)
- [Elsa.Studio.Authentication.ElsaAuth.UI README](../src/modules/Elsa.Studio.Authentication.ElsaAuth.UI/README.md)
- [Microsoft Authentication Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)

## Future Enhancements

Potential future additions:

- Automatic provider discovery from configuration
- Multi-tenant authentication support
- Authentication caching and performance optimizations
- Enhanced token refresh strategies
- Authentication event hooks and middleware
- Support for additional identity providers (Auth0, Okta, etc.)
