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
│  OIDC Provider       │      │  OAuth2 Provider │   │ Future      │
│  • IOidcTokenAccessor│      │  (Future)        │   │ Providers   │
│  • OidcOptions       │      │                  │   │ (JWT, SAML) │
│  • Server & WASM     │      │                  │   │             │
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

Multiple authentication providers can be registered simultaneously:

```csharp
// Example: Register OIDC for API calls, JWT for specific endpoints
services.AddOidcAuthentication(options => { /* OIDC config */ });
services.AddJwtAuthentication(options => { /* JWT config */ });

// The manager will try each provider until a valid token is found
```

### 3. Hosting Model Differences

#### Blazor Server
- Uses ASP.NET Core authentication middleware
- Tokens stored server-side in authentication properties
- Accessed via `HttpContext.GetTokenAsync()`
- Cookie-based session management
- No client-side token exposure

#### Blazor WebAssembly
- Uses `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- Tokens managed by browser-based authentication framework
- Accessed via `IAccessTokenProvider`
- Automatic token refresh before expiry
- Secure token storage in browser

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

#### 1. Elsa.Studio.Login (Legacy)
- **Location**: `src/modules/Elsa.Studio.Login`
- **Supports**: OIDC, OAuth2, Elsa Identity
- **Status**: Maintained for backward compatibility
- **Note**: Tight coupling with general login functionality

#### 2. Elsa.Studio.Authentication.OpenIdConnect (New)
- **Location**: `src/modules/Elsa.Studio.Authentication.OpenIdConnect`
- **Supports**: OpenID Connect
- **Hosting**: Separate packages for Server and WASM
- **Features**:
  - Uses Microsoft's built-in OIDC handlers
  - Automatic token refresh
  - PKCE support
  - Cookie-based auth (Server) or framework-managed (WASM)

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
// In Elsa.Studio.Login
public class AuthenticatingApiHttpMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(...)
    {
        var token = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
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
- ✅ Tokens never exposed to client browser
- ✅ Cookie-based authentication with HTTP-only cookies
- ✅ Secure server-side session management
- ✅ HTTPS-only cookies in production

### WASM Hosting
- ✅ Tokens managed by authentication framework
- ✅ Automatic token expiry and renewal
- ✅ Access tokens available, but refresh tokens hidden
- ✅ Uses standard browser security features

### General
- ✅ PKCE enabled by default for OIDC
- ✅ HTTPS required for metadata endpoints
- ✅ Token refresh on 401 responses
- ✅ Secure token storage per hosting model

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

**Blazor Server**:
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

**Blazor WASM**:
```csharp
builder.Services.AddOidcAuthentication(options =>
{
    options.Authority = "https://identity-server.com";
    options.ClientId = "elsa-studio-wasm";
    options.Scopes = new[] { "openid", "profile", "elsa_api" };
});
```

## Migration Path

### From Elsa.Studio.Login to New Providers

The new authentication providers are designed to coexist with the legacy `Elsa.Studio.Login`:

1. **Phase 1**: Add new provider alongside existing Login module
2. **Phase 2**: Test new provider with your identity server
3. **Phase 3**: Switch to new provider by removing Login module registration
4. **Phase 4**: Remove Login module dependency once stable

No breaking changes to existing applications.

## Troubleshooting

### Common Issues

1. **Multiple providers registered, wrong one used**
   - `IAuthenticationProviderManager` returns first valid token
   - Check registration order
   - Ensure only desired provider is registered

2. **Tokens not available in WASM**
   - Only access tokens directly accessible
   - Refresh/ID tokens managed by framework

3. **401 errors on API calls**
   - Check token scopes match API requirements
   - Verify `AuthenticatingApiHttpMessageHandler` is registered
   - Check identity server returns correct audience

4. **SignalR connections fail**
   - Ensure `offline_access` scope for refresh tokens
   - Verify token provider returns valid token
   - Check SignalR hub authentication configuration

## Resources

- [Elsa.Studio.Authentication.Abstractions README](../modules/Elsa.Studio.Authentication.Abstractions/README.md)
- [Elsa.Studio.Authentication.OpenIdConnect README](../modules/Elsa.Studio.Authentication.OpenIdConnect/README.md)
- [Microsoft Authentication Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)

## Future Enhancements

Potential future additions:

- Automatic provider discovery from configuration
- Multi-tenant authentication support
- Authentication caching and performance optimizations
- Enhanced token refresh strategies
- Authentication event hooks and middleware
- Support for additional identity providers (Auth0, Okta, etc.)
