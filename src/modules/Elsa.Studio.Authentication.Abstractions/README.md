# Elsa Studio Authentication Abstractions

Shared abstractions for authentication providers in Elsa Studio.

## Overview

This package provides common interfaces and base classes that can be shared across different authentication provider implementations (OIDC, OAuth2, JWT, SAML, etc.). It promotes consistency and reusability across authentication modules.

## Purpose

The abstractions package allows:

1. **Multiple Authentication Providers**: Support various authentication mechanisms without duplicating code
2. **Consistent Patterns**: Provide a common token access pattern across all providers
3. **Extensibility**: Make it easy to add new authentication providers
4. **Decoupling**: Keep provider-specific code separate while maintaining shared contracts

## Abstractions

### ITokenAccessor

The core abstraction for retrieving authentication tokens from any authentication provider.

```csharp
public interface ITokenAccessor
{
    Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default);
}
```

**Usage**: Authentication providers implement this interface to provide access to tokens stored in their specific context (HTTP context, browser storage, etc.).

### AuthenticationOptions

A base class for authentication configuration that can be extended by specific providers.

```csharp
public abstract class AuthenticationOptions
{
    public string[] Scopes { get; set; }
    public bool RequireHttpsMetadata { get; set; }
}
```

**Usage**: Provider-specific options classes inherit from this to add their own configuration properties.

## Example: Implementing a New Authentication Provider

### 1. Create Provider-Specific Options

```csharp
using Elsa.Studio.Authentication.Abstractions.Models;

public class CustomAuthOptions : AuthenticationOptions
{
    public string Authority { get; set; }
    public string ClientId { get; set; }
    // Add provider-specific properties
}
```

### 2. Implement ITokenAccessor

```csharp
using Elsa.Studio.Authentication.Abstractions.Contracts;

public class CustomTokenAccessor : ITokenAccessor
{
    public async Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken)
    {
        // Retrieve token from your provider's storage/context
        return await GetTokenFromCustomSource(tokenName);
    }
}
```

### 3. Implement IAuthenticationProvider

```csharp
using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.Abstractions.Contracts;

public class CustomAuthenticationProvider : IAuthenticationProvider
{
    private readonly ITokenAccessor _tokenAccessor;

    public CustomAuthenticationProvider(ITokenAccessor tokenAccessor)
    {
        _tokenAccessor = tokenAccessor;
    }

    public async Task<string?> GetAccessTokenAsync(string tokenName, CancellationToken cancellationToken)
    {
        // Map standard token names to provider-specific names if needed
        return await _tokenAccessor.GetTokenAsync(tokenName, cancellationToken);
    }
}
```

### 4. Register Services

```csharp
services.AddScoped<ITokenAccessor, CustomTokenAccessor>();
services.AddScoped<IAuthenticationProvider, CustomAuthenticationProvider>();
```

## Existing Implementations

The following authentication providers use these abstractions:

- **Elsa.Studio.Authentication.OpenIdConnect** - OpenID Connect authentication
  - `IOidcTokenAccessor : ITokenAccessor` - OIDC-specific token accessor
  - `OidcOptions : AuthenticationOptions` - OIDC configuration
  - Implementations for Blazor Server and WebAssembly

## Design Philosophy

### Why Abstractions?

1. **Separation of Concerns**: Core token access logic is separated from provider-specific implementations
2. **Testability**: Easy to mock token accessors for unit testing
3. **Flexibility**: New authentication providers can be added without modifying existing code
4. **Future-Proof**: Changes to one provider don't affect others

### Why Not More Abstractions?

We intentionally keep the abstraction layer minimal:

- Different authentication providers have significantly different flows
- Over-abstraction can make implementations harder to understand
- Provider-specific optimizations should not be constrained by abstractions
- The `IAuthenticationProvider` interface in `Elsa.Studio.Core` is already very flexible

## Integration with Elsa Studio Core

These abstractions work alongside the existing authentication infrastructure:

```
Elsa.Studio.Core
├── IAuthenticationProvider          ← Called by Elsa Studio
│   └── Implemented by providers     ← Uses ITokenAccessor internally
│
Elsa.Studio.Authentication.Abstractions
├── ITokenAccessor                   ← Provider-agnostic token access
└── AuthenticationOptions            ← Shared configuration
```

## Relationship with IAuthenticationProviderManager

The `IAuthenticationProviderManager` (from `Elsa.Studio.Core`) iterates through registered `IAuthenticationProvider` implementations to find a valid token. This allows multiple authentication providers to coexist, with the manager selecting the first one that returns a token.

## Future Extensions

Potential additions to the abstractions:

- `ITokenRefreshHandler` - For providers that support token refresh
- `IAuthenticationStateProvider` - For providers that need custom authentication state
- `IAuthenticationEventHandler` - For handling sign-in/sign-out events

These would be added only when multiple providers require the same pattern.

## License

This package is part of Elsa Studio and follows the same license terms.
