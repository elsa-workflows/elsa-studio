# Elsa.Studio.Authentication.ElsaAuth.BlazorServer

Blazor Server hosting implementation for Elsa Identity (JWT-based) authentication in Elsa Studio.

## Overview

This module provides the Blazor Server-specific services for JWT-based authentication with Elsa Identity. It extends the core `Elsa.Studio.Authentication.ElsaAuth` module with:

- **Prerendering-aware token storage** - Safely handles tokens during server-side prerendering
- **Server-side JWT accessor** - Stores tokens in browser local storage with HTTP context awareness
- **HTTP context integration** - Uses `IHttpContextAccessor` to detect prerendering state

## What You Get

- `BlazorServerJwtAccessor` - Blazor Server implementation of `IJwtAccessor` with prerendering support
- Automatic registration of all core ElsaAuth services
- HTTP context accessor for server-side rendering detection

## Installation

Add a package reference to your Blazor Server project:

```xml
<PackageReference Include="Elsa.Studio.Authentication.ElsaAuth.BlazorServer" />
```

## Usage

### Basic Setup

In your Blazor Server `Program.cs`:

```csharp
using Elsa.Studio.Authentication.ElsaAuth.BlazorServer.Extensions;

// Add ElsaAuth services with Blazor Server support
builder.Services.AddElsaAuth();
```

This single call registers:
- All core ElsaAuth services from `Elsa.Studio.Authentication.ElsaAuth`
- `BlazorServerJwtAccessor` as the `IJwtAccessor` implementation
- `IHttpContextAccessor` for prerendering detection

### Complete Authentication Setup

For a complete authentication setup, also add the UI module:

```csharp
using Elsa.Studio.Authentication.ElsaAuth.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaAuth.UI.Extensions;

// 1. Platform services (Blazor Server)
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

### Blazor Server-Specific Considerations

#### Prerendering Support

Blazor Server prerenders components on the server before establishing a SignalR circuit. During prerendering:
- No browser context is available
- Local storage cannot be accessed
- HTTP response has not started yet

`BlazorServerJwtAccessor` handles this by:

```csharp
protected override bool CanAccessStorage() => !IsPrerendering();

private bool IsPrerendering() => httpContextAccessor.HttpContext?.Response.HasStarted == false;
```

When prerendering is detected, token storage operations are safely skipped, preventing errors.

#### Token Storage

Tokens are stored in **browser local storage** (via `Blazored.LocalStorage`), not server-side storage. This means:
- Tokens persist across browser sessions
- Tokens are stored per-browser, not per-user on the server
- Tokens are not accessible during prerendering

### Component Structure

```
Elsa.Studio.Authentication.ElsaAuth/                (Core services)
├── Contracts/
│   ├── IJwtAccessor.cs                             (Platform abstraction)
│   └── ...other core contracts
└── Services/
    └── JwtAccessorBase.cs                           (Base implementation)

Elsa.Studio.Authentication.ElsaAuth.BlazorServer/    (This module)
└── Services/
    └── BlazorServerJwtAccessor.cs                   (Server implementation)
```

## Dependencies

This module depends on:
- `Elsa.Studio.Authentication.ElsaAuth` - Core authentication services
- `Blazored.LocalStorage` - Browser local storage access
- `Microsoft.AspNetCore.Http.Abstractions` - HTTP context access

## Differences from Blazor WebAssembly

| Aspect | Blazor Server | Blazor WebAssembly |
|--------|--------------|-------------------|
| Prerendering | Yes, requires detection | No prerendering |
| HTTP Context | Available via `IHttpContextAccessor` | Not available |
| Storage Access | Must check for prerendering | Always available |
| Token Location | Browser local storage | Browser local storage |
| Runtime | Server-side with SignalR | Client-side in browser |

## Integration with Other Modules

### With ElsaAuth.UI

For a complete authentication experience, combine with the UI module:

```csharp
// Blazor Server host
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

### "Cannot access local storage during prerendering"

This error should not occur with this module, as `BlazorServerJwtAccessor` prevents storage access during prerendering. If you see this error:

1. Ensure you're using `AddElsaAuth()` from this module, not manually registering services
2. Check that `IHttpContextAccessor` is registered (automatically done by `AddElsaAuth()`)
3. Verify no other code is directly accessing local storage during prerendering

### Tokens not persisting after server restart

This is expected behavior. Tokens are stored in the **browser**, not on the server:
- Tokens persist across server restarts
- Tokens persist across browser refreshes
- Tokens are lost when browser storage is cleared

### Authentication state lost during prerendering

Components that depend on authentication state should defer rendering until after prerendering:

```razor
@if (!IsPrerendering)
{
    <AuthorizeView>
        <Authorized>
            @* Your authenticated content *@
        </Authorized>
    </AuthorizeView>
}
```

Or use the `@attribute [Authorize]` directive to require authentication before rendering.

## Related Modules

- **`Elsa.Studio.Authentication.ElsaAuth`** - Core authentication services (required)
- **`Elsa.Studio.Authentication.ElsaAuth.UI`** - Login UI components (recommended)
- **`Elsa.Studio.Authentication.ElsaAuth.BlazorWasm`** - WebAssembly alternative to this module
- **`Elsa.Studio.Authentication.OpenIdConnect.BlazorServer`** - OpenID Connect alternative

## See Also

- [Elsa.Studio.Authentication.ElsaAuth README](../Elsa.Studio.Authentication.ElsaAuth/README.md) - Core authentication documentation
- [Elsa.Studio.Authentication.ElsaAuth.UI README](../Elsa.Studio.Authentication.ElsaAuth.UI/README.md) - UI module documentation
