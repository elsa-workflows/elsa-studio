using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.Components;
using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Login.ComponentProviders;

/// <inheritdoc />
public class RedirectToLoginUnauthorizedComponentProvider(IServiceProvider serviceProvider) : IUnauthorizedComponentProvider
{
    /// <inheritdoc />
    public RenderFragment GetUnauthorizedComponent()
    {
        // The legacy Login module requires an IAuthorizationService to perform the redirect.
        // When using the newer authentication providers (e.g. the OIDC module), this service will not be registered.
        // In that case, fall back to the default Unauthorized component (no redirect).
        var authorizationService = serviceProvider.GetService<IAuthorizationService>();

        return authorizationService != null
            ? builder => builder.CreateComponent<RedirectToLogin>()
            : builder => builder.CreateComponent<Unauthorized>();
    }
}