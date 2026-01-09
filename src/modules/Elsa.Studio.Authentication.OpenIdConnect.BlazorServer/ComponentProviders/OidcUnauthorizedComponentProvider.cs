using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.ComponentProviders;

/// <summary>
/// Provides an unauthorized component that initiates an OpenID Connect challenge.
/// </summary>
public class OidcUnauthorizedComponentProvider : IUnauthorizedComponentProvider
{
    /// <inheritdoc />
    public RenderFragment GetUnauthorizedComponent() => builder => builder.CreateComponent<ChallengeToLogin>();
}

