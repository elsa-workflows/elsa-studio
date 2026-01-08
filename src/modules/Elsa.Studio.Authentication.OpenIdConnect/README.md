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

## Security Considerations

- **Server**: Uses secure, HTTP-only cookies. Tokens never exposed to browser.
- **WASM**: Tokens managed by framework with proper expiry and renewal.
- **PKCE**: Enabled by default to protect against authorization code interception.
- **HTTPS**: Required for metadata endpoints in production (configurable for dev).

## Related Packages

- `Elsa.Studio.Authentication.Abstractions` (Shared authentication abstractions)
- `Microsoft.AspNetCore.Authentication.OpenIdConnect` (Server)
- `Microsoft.AspNetCore.Components.WebAssembly.Authentication` (WASM)
- `Elsa.Studio.Core` (Core interfaces)

## Building Your Own Authentication Provider

If you need to implement a different authentication mechanism (OAuth2, JWT, SAML, etc.), refer to the `Elsa.Studio.Authentication.Abstractions` package documentation for guidance on creating new authentication providers that follow the same patterns.

## License

This module is part of Elsa Studio and follows the same license terms.
