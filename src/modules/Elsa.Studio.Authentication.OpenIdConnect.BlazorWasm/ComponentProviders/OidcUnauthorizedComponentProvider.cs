using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.ComponentProviders;

/// <summary>
/// Provides an unauthorized component that navigates to the built-in WASM OIDC login endpoint.
/// </summary>
public class OidcUnauthorizedComponentProvider : IUnauthorizedComponentProvider
{
    /// <inheritdoc />
    public RenderFragment GetUnauthorizedComponent() => builder => builder.CreateComponent<NavigateToLogin>();
}

