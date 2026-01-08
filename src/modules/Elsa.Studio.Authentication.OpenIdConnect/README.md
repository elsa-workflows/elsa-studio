# Elsa Studio Authentication - OpenID Connect

A modern, best-practices OpenID Connect (OIDC) authentication module for Elsa Studio that leverages Microsoft's built-in authentication infrastructure.

## Overview

This module provides a clean, decoupled alternative to the OIDC implementation in `Elsa.Studio.Login`. It uses Microsoft's native authentication packages for automatic token management, PKCE support, and proper integration with ASP.NET Core and Blazor frameworks.

**Note**: This is one of potentially many authentication providers for Elsa Studio. It extends the shared `Elsa.Studio.Authentication.Abstractions` to provide OIDC-specific functionality.

### Key Benefits

- **Automatic Token Management**: Tokens are automatically refreshed by the framework
- **Built-in PKCE Support**: Uses Microsoft's built-in Proof Key for Code Exchange implementation
- **Proper Middleware Integration**: Integrates with ASP.NET Core authentication pipeline
- **Hosting Model Optimized**: Separate implementations for Blazor Server and WebAssembly
- **Clean Architecture**: No browser storage manipulation, uses framework-managed authentication state
- **Security Best Practices**: Cookie-based sessions for Server, secure token provider for WASM
- **Shared Abstractions**: Uses common patterns from `Elsa.Studio.Authentication.Abstractions`

## Architecture

### Project Structure

```
Elsa.Studio.Authentication.Abstractions/
├── Contracts/
│   └── ITokenAccessor.cs               # Shared token accessor abstraction
└── Models/
    └── AuthenticationOptions.cs        # Base authentication options

Elsa.Studio.Authentication.OpenIdConnect/
├── Contracts/
│   └── IOidcTokenAccessor.cs           # OIDC-specific token accessor (extends ITokenAccessor)
├── Models/
│   └── OidcOptions.cs                  # OIDC configuration (extends AuthenticationOptions)
└── Services/
    └── OidcAuthenticationProvider.cs   # IAuthenticationProvider implementation

Elsa.Studio.Authentication.OpenIdConnect.BlazorServer/
├── Extensions/
│   └── ServiceCollectionExtensions.cs  # Server DI setup
└── Services/
    └── ServerOidcTokenAccessor.cs      # Server-side token accessor

Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm/
├── Extensions/
│   └── ServiceCollectionExtensions.cs  # WASM DI setup
└── Services/
    └── WasmOidcTokenAccessor.cs        # WASM-side token accessor
```

### Design Decisions

#### Blazor Server Implementation
- Uses `Microsoft.AspNetCore.Authentication.OpenIdConnect` middleware
- Cookie-based authentication with secure session management
- Tokens stored in authentication properties (server-side only)
- Retrieved via `HttpContext.GetTokenAsync()` - no client storage

#### Blazor WebAssembly Implementation
- Uses `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- Leverages built-in `IAccessTokenProvider` for automatic token management
- Framework handles token refresh, expiry, and renewal automatically
- Tokens never exposed directly to application code (security by design)

## Installation & Usage

### Blazor Server

1. **Add Package References** (via project references or NuGet):
   ```xml
   <PackageReference Include="Elsa.Studio.Authentication.OpenIdConnect.BlazorServer" />
   ```

2. **Configure in `Program.cs`**:
   ```csharp
   using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;

   // Configure OIDC authentication
   builder.Services.AddOidcAuthentication(options =>
   {
       options.Authority = "https://your-identity-server.com";
       options.ClientId = "elsa-studio";
       options.ClientSecret = "your-client-secret"; // Optional for confidential clients
       options.Scopes = new[] { "openid", "profile", "elsa_api", "offline_access" };
       options.UsePkce = true; // Recommended
   });
   ```

3. **Add Authentication Middleware**:
   ```csharp
   // Before app.UseRouting()
   app.UseAuthentication();
   app.UseAuthorization();
   ```

4. **Protect Pages** (optional):
   ```csharp
   // In _Imports.razor or individual pages
   @attribute [Authorize]
   ```

### Blazor WebAssembly

1. **Add Package References**:
   ```xml
   <PackageReference Include="Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm" />
   ```

2. **Configure in `Program.cs`**:
   ```csharp
   using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;

   // Configure OIDC authentication
   builder.Services.AddOidcAuthentication(options =>
   {
       options.Authority = "https://your-identity-server.com";
       options.ClientId = "elsa-studio-wasm";
       options.Scopes = new[] { "openid", "profile", "elsa_api", "offline_access" };
       options.ResponseType = "code";
       options.CallbackPath = "/authentication/login-callback";
       options.SignedOutCallbackPath = "/authentication/logout-callback";
   });
   ```

3. **Add Authentication Components** in `App.razor`:
   ```razor
   <CascadingAuthenticationState>
       <Router AppAssembly="@typeof(App).Assembly">
           <Found Context="routeData">
               <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                   <NotAuthorized>
                       <RedirectToLogin />
                   </NotAuthorized>
               </AuthorizeRouteView>
           </Found>
       </Router>
   </CascadingAuthenticationState>
   ```

4. **Add Authentication Routes**:
   Create `Authentication.razor`:
   ```razor
   @page "/authentication/{action}"
   @using Microsoft.AspNetCore.Components.WebAssembly.Authentication
   <RemoteAuthenticatorView Action="@Action" />

   @code {
       [Parameter] public string? Action { get; set; }
   }
   ```

## Configuration Options

### OidcOptions

`OidcOptions` extends `AuthenticationOptions` from `Elsa.Studio.Authentication.Abstractions`.

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `Authority` | `string` | OIDC provider authority URL | Required |
| `ClientId` | `string` | Client ID registered with provider | Required |
| `ClientSecret` | `string?` | Client secret (Server only, optional) | `null` |
| `ResponseType` | `string` | OAuth2 response type | `"code"` |
| `Scopes` | `string[]` | Requested scopes (inherited) | `["openid", "profile", "offline_access"]` |
| `UsePkce` | `bool` | Enable PKCE | `true` |
| `SaveTokens` | `bool` | Save tokens in auth properties (Server) | `true` |
| `CallbackPath` | `string` | Authentication callback path | `"/signin-oidc"` |
| `SignedOutCallbackPath` | `string` | Sign-out callback path | `"/signout-callback-oidc"` |
| `RequireHttpsMetadata` | `bool` | Require HTTPS for metadata (inherited) | `true` |
| `GetClaimsFromUserInfoEndpoint` | `bool` | Fetch claims from UserInfo | `true` |
| `MetadataAddress` | `string?` | Custom metadata address | Auto-discovered |

## Token Access

Both implementations provide access to tokens via the standard `IAuthenticationProvider` interface:

```csharp
@inject IAuthenticationProviderManager AuthProviderManager

var accessToken = await AuthProviderManager.GetAuthenticationTokenAsync(TokenNames.AccessToken);
```

### Token Names

- `TokenNames.AccessToken` - Access token for API calls
- `TokenNames.IdToken` - ID token (Server only)
- `TokenNames.RefreshToken` - Refresh token (Server only, if available)

> **Note**: In Blazor WASM, only access tokens are directly accessible. ID and refresh tokens are managed internally by the framework for security.

## SignalR Integration

The module works seamlessly with `WorkflowInstanceObserverFactory` for SignalR connections:

```csharp
// Token is automatically retrieved and applied to SignalR hub connections
var observer = await observerFactory.CreateAsync(workflowInstanceId);
```

The `IAuthenticationProviderManager` automatically retrieves the access token from the appropriate source (HTTP context for Server, token provider for WASM).

## Migration from Elsa.Studio.Login

If you're currently using the OIDC implementation in `Elsa.Studio.Login`, here's how to migrate:

### Before (Blazor Server):
```csharp
builder.Services.AddLoginModule();
builder.Services.UseOpenIdConnect(options =>
{
    options.AuthEndpoint = "https://identity-server.com/connect/authorize";
    options.TokenEndpoint = "https://identity-server.com/connect/token";
    options.EndSessionEndpoint = "https://identity-server.com/connect/endsession";
    options.ClientId = "elsa-studio";
    options.ClientSecret = "secret";
    options.Scopes = new[] { "openid", "profile", "elsa_api" };
});
```

### After (Blazor Server):
```csharp
builder.Services.AddOidcAuthentication(options =>
{
    options.Authority = "https://identity-server.com"; // Auto-discovers endpoints
    options.ClientId = "elsa-studio";
    options.ClientSecret = "secret";
    options.Scopes = new[] { "openid", "profile", "elsa_api", "offline_access" };
});

// Add middleware
app.UseAuthentication();
app.UseAuthorization();
```

## Differences from Legacy Implementation

| Feature | Legacy (Elsa.Studio.Login) | New (This Module) |
|---------|---------------------------|-------------------|
| Token Storage | Browser LocalStorage/SessionStorage | Server: Auth properties (server-side only)<br>WASM: Framework-managed |
| Token Refresh | Manual with custom service | Automatic via framework |
| PKCE | Manual implementation | Built-in framework support |
| Middleware | Custom authorization redirect | Standard ASP.NET Core auth pipeline |
| Token Access | Direct storage access | Via `IAuthenticationProvider` abstraction |
| Security | Tokens exposed in browser storage | Server: Session cookies only<br>WASM: Framework-secured |

## Troubleshooting

### Common Issues

1. **"SaveTokens must be true" error**:
   - Ensure `SaveTokens = true` in Server configuration
   - This is required for token retrieval via `HttpContext.GetTokenAsync()`

2. **Tokens not available in WASM**:
   - Only access tokens are directly accessible in WASM
   - ID and refresh tokens are managed by the framework for security

3. **SignalR connections fail**:
   - Ensure the `offline_access` scope is requested for refresh tokens
   - Verify the identity provider returns tokens with appropriate audience

4. **Pre-rendering issues in Server**:
   - Tokens are not available during pre-rendering
   - Use `@attribute [Authorize]` to ensure authentication before render

## Silent access token refresh (Blazor Server)

On Blazor Server, Elsa Studio uses the standard ASP.NET Core Cookie + OpenID Connect handler.
When you set `SaveTokens = true`, the handler stores `access_token`, `refresh_token` (if issued) and `expires_at` in the authentication cookie properties.

This module can **silently refresh the access token** when it is about to expire by:
- reading `expires_at` / `refresh_token` from the auth cookie
- calling the OIDC provider's `token_endpoint` using the `refresh_token` grant
- updating the tokens stored in the auth cookie (via `SignInAsync`)

### Flip-a-switch configuration

The only thing you need to do to enable silent refresh is to keep refresh tokens enabled and request `offline_access`:

```csharp
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;

builder.Services.AddOidcAuthentication(options =>
{
    options.Authority = "https://login.microsoftonline.com/{tenantId}/v2.0"; // or your provider
    options.ClientId = "...";

    // Required if you want the handler to store tokens in the auth cookie.
    options.SaveTokens = true;

    // Required if you want refresh tokens.
    // Note: some providers/app registrations might not issue a refresh token even if requested.
    options.Scopes = new[] { "openid", "profile", "offline_access" };
});

// Optional: control refresh behavior.
builder.Services.Configure<OidcTokenRefreshOptions>(options =>
{
    options.EnableRefreshTokens = true; // default
    options.RefreshSkew = TimeSpan.FromMinutes(2); // default
});
```

### Prerequisites and behavior

- `SaveTokens` must be `true` (otherwise no tokens are available in the auth cookie).
- `offline_access` should be requested (otherwise a refresh token is typically not issued).
- **Strategy**:
  - `BestEffort` (default): the module only renews the auth cookie when response headers can still be written.
  - `Persisted`: the module renews the auth cookie via the dedicated `POST /authentication/refresh` endpoint.
- **Blazor Server note**: once the initial page load is complete, most calls happen over the SignalR circuit where HTTP headers have already been sent. In that context, cookies cannot be updated.
  - With `BestEffort`, this means the app will eventually fall back to a normal OIDC re-authentication when the access token expires.
  - With `Persisted`, you should periodically call the refresh endpoint (e.g., a background ping) so renewal happens in a non-circuit HTTP request.
- If no refresh token is available, or refresh fails, the module does not throw; the next API call will typically result in a normal auth challenge.

### Microsoft Entra ID notes

For Microsoft Entra ID (Azure AD), the authority usually looks like:
- Tenant-specific: `https://login.microsoftonline.com/{tenantId}/v2.0`
- Or common endpoint (multi-tenant apps): `https://login.microsoftonline.com/common/v2.0`

Refresh tokens depend on:
- requesting `offline_access`
- your app registration configuration / consent
- Entra token policies (token lifetimes, session policies)

If you don't receive a refresh token, the app will still work, but access-token renewal will require re-authentication.

### Advanced overrides (rarely needed)

By default, the module auto-discovers the `token_endpoint` from OIDC metadata and uses the configured `ClientId`/`ClientSecret` from `AddOidcAuthentication`.

You can override any of these via `OidcTokenRefreshOptions`:

```csharp
builder.Services.Configure<OidcTokenRefreshOptions>(options =>
{
    options.EnableRefreshTokens = true;

    // Override token endpoint discovery.
    options.TokenEndpoint = "https://issuer.example.com/oauth2/v2.0/token";

    // Override client credentials.
    options.ClientId = "...";
    options.ClientSecret = "..."; // optional
});
```

### Host configuration example (appsettings.json)

In the Blazor Server host, you can enable persisted silent refresh purely via configuration:

```json
{
  "Authentication": {
    "Provider": "OpenIdConnect",
    "OpenIdConnect": {
      "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
      "ClientId": "...",
      "ClientSecret": "...",
      "Scopes": ["openid", "profile", "offline_access"],
      "SaveTokens": true,
      "TokenRefresh": {
        "Strategy": "Persisted",
        "Ping": {
          "RefreshEndpointPath": "/authentication/refresh",
          "Interval": "00:01:00"
        }
      }
    }
  }
}
```

> Set `TokenRefresh:Strategy` to `BestEffort` (or omit it) to disable persisted refresh.
