# Elsa.Studio.Authentication.ElsaAuth.BlazorWasm

Blazor WebAssembly hosting implementation for Elsa Identity (JWT-based) authentication in Elsa Studio.

## Overview

This module provides the Blazor WebAssembly-specific services for JWT-based authentication with Elsa Identity. It extends the core `Elsa.Studio.Authentication.ElsaAuth` module with:

- **Client-side JWT accessor** - Stores tokens in browser local storage
- **WebAssembly-optimized implementation** - No server-side dependencies or prerendering concerns
- **Simplified token management** - Direct browser storage access without HTTP context

## What You Get

- `BlazorWasmJwtAccessor` - Blazor WebAssembly implementation of `IJwtAccessor`
- Automatic registration of all core ElsaAuth services
- Client-side token storage in browser local storage

## Installation

Add a package reference to your Blazor WebAssembly project:

```xml
<PackageReference Include="Elsa.Studio.Authentication.ElsaAuth.BlazorWasm" />
```

## Usage

### Basic Setup

In your Blazor WebAssembly `Program.cs`:

```csharp
using Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Extensions;

// Add ElsaAuth services with Blazor WebAssembly support
builder.Services.AddElsaAuth();
```

This single call registers:
- All core ElsaAuth services from `Elsa.Studio.Authentication.ElsaAuth`
- `BlazorWasmJwtAccessor` as the `IJwtAccessor` implementation

### Complete Authentication Setup

For a complete authentication setup, also add the UI module:

```csharp
using Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Extensions;
using Elsa.Studio.Authentication.ElsaAuth.UI.Extensions;

// 1. Platform services (Blazor WebAssembly)
builder.Services.AddElsaAuth();

// 2. UI components (login page, unauthorized handler)
builder.Services.AddElsaAuthUI();
```

### Configuration

Configure Elsa Identity endpoints if they differ from defaults:

```csharp
builder.Services.Configure<IdentityTokenOptions>(options =>
{
    options.LoginUrl = "https://your-elsa-api.com/identity/login";
    options.RefreshUrl = "https://your-elsa-api.com/identity/refresh";
});
```

## Architecture

### Blazor WebAssembly-Specific Considerations

#### No Prerendering

Unlike Blazor Server, WebAssembly apps run entirely in the browser:
- No server-side prerendering
- Browser storage is always available
- No HTTP context or SignalR circuit

This simplifies the implementation significantly. `BlazorWasmJwtAccessor` is a minimal wrapper:

```csharp
public class BlazorWasmJwtAccessor(ILocalStorageService localStorageService)
    : JwtAccessorBase(localStorageService);
```

It inherits all functionality from `JwtAccessorBase` without needing prerendering checks.

#### Token Storage

Tokens are stored in **browser local storage** (via `Blazored.LocalStorage`):
- Tokens persist across browser sessions
- Tokens are stored locally in the browser
- Tokens are accessible immediately on app startup

### Component Structure

```
Elsa.Studio.Authentication.ElsaAuth/                (Core services)
├── Contracts/
│   ├── IJwtAccessor.cs                             (Platform abstraction)
│   └── ...other core contracts
└── Services/
    └── JwtAccessorBase.cs                           (Base implementation)

Elsa.Studio.Authentication.ElsaAuth.BlazorWasm/     (This module)
└── Services/
    └── BlazorWasmJwtAccessor.cs                     (WebAssembly implementation)
```

## Dependencies

This module depends on:
- `Elsa.Studio.Authentication.ElsaAuth` - Core authentication services
- `Blazored.LocalStorage` - Browser local storage access

## Differences from Blazor Server

| Aspect | Blazor WebAssembly | Blazor Server |
|--------|-------------------|---------------|
| Prerendering | No prerendering | Yes, requires detection |
| HTTP Context | Not available | Available via `IHttpContextAccessor` |
| Storage Access | Always available | Must check for prerendering |
| Token Location | Browser local storage | Browser local storage |
| Runtime | Client-side in browser | Server-side with SignalR |
| Implementation | Simple wrapper | Prerendering-aware logic |

## Integration with Other Modules

### With ElsaAuth.UI

For a complete authentication experience, combine with the UI module:

```csharp
// Blazor WebAssembly host
builder.Services.AddElsaAuth();           // This module
builder.Services.AddElsaAuthUI();         // Login UI
```

This provides:
- JWT authentication services (this module)
- Login page at `/login` (UI module)
- Unauthorized redirect handling (UI module)

### With Elsa Studio Core

The authentication services integrate seamlessly with Elsa Studio:

```csharp
// Configure Elsa Studio with ElsaAuth
builder.Services.AddElsaStudio(elsa =>
{
    elsa.AddBackend(backend => backend.Url = "https://your-elsa-api.com");
});

// Add authentication
builder.Services.AddElsaAuth();
builder.Services.AddElsaAuthUI();
```

All HTTP requests to the Elsa backend API automatically include JWT bearer tokens via `ElsaAuthAuthenticatingApiHttpMessageHandler`.

## Troubleshooting

### Tokens not persisting across sessions

Ensure browser local storage is enabled:
1. Check browser console for local storage errors
2. Verify the browser allows local storage for your domain
3. Check browser privacy/security settings

### Authentication state not updating after login

The authentication state should update automatically after tokens are written. If not:

1. Ensure `AccessTokenAuthenticationStateProvider` is properly registered
2. Check that tokens are being written to local storage (check browser DevTools > Application > Local Storage)
3. Verify the JWT contains valid claims

### CORS errors when calling Elsa backend

Blazor WebAssembly makes direct HTTP requests from the browser, which requires proper CORS configuration on the Elsa backend:

```csharp
// In Elsa backend API Program.cs
builder.Services.AddCors(cors =>
{
    cors.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("https://your-blazor-wasm-app.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

app.UseCors();
```

### Tokens cleared on browser refresh

If tokens are being cleared unexpectedly:
1. Verify you're using `Blazored.LocalStorage`, not `SessionStorage`
2. Check for code that explicitly clears tokens on startup
3. Review browser DevTools > Application > Local Storage to confirm tokens persist

## Security Considerations

### Token Storage in Browser

Tokens are stored in browser local storage, which means:
- ✅ Tokens persist across browser sessions
- ✅ Tokens survive page refreshes
- ⚠️ Tokens are accessible to JavaScript code (XSS risk)
- ⚠️ Tokens are not protected by HTTP-only cookies

**Best practices:**
- Use HTTPS to protect tokens in transit
- Implement short token lifetimes with refresh tokens
- Sanitize all user input to prevent XSS attacks
- Consider using OpenID Connect with HTTP-only cookies for higher security requirements

### XSS Protection

To minimize XSS risk:
1. Always sanitize user-provided content before rendering
2. Use Blazor's built-in HTML encoding (automatic in Razor)
3. Avoid using `@((MarkupString)unsafeHtml)` with untrusted content
4. Implement Content Security Policy (CSP) headers
5. Keep token lifetimes short and use refresh tokens

## Related Modules

- **`Elsa.Studio.Authentication.ElsaAuth`** - Core authentication services (required)
- **`Elsa.Studio.Authentication.ElsaAuth.UI`** - Login UI components (recommended)
- **`Elsa.Studio.Authentication.ElsaAuth.BlazorServer`** - Server alternative to this module
- **`Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm`** - OpenID Connect alternative

## See Also

- [Elsa.Studio.Authentication.ElsaAuth README](../Elsa.Studio.Authentication.ElsaAuth/README.md) - Core authentication documentation
- [Elsa.Studio.Authentication.ElsaAuth.UI README](../Elsa.Studio.Authentication.ElsaAuth.UI/README.md) - UI module documentation
