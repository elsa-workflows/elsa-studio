using Elsa.Studio.Authentication.Abstractions.Contracts;

namespace Elsa.Studio.Authentication.OpenIdConnect.Contracts;

/// <summary>
/// Provides access to OIDC tokens stored in the authentication context.
/// Extends <see cref="ITokenAccessor"/> with OIDC-specific functionality if needed.
/// </summary>
public interface IOidcTokenAccessor : ITokenAccessor
{
    // Can add OIDC-specific methods here in the future if needed
}
