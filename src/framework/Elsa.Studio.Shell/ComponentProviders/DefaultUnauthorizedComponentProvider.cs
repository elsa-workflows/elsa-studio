using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Shell.ComponentProviders;

/// <summary>
/// Provides a <see cref="RenderFragment"/> that displays nothing. This is useful for scenarios where the login page is not required. If you need to display a login page, use <c>LoginPageProvider"</c> from the Login module instead or implement your own <see cref="IUnauthorizedComponentProvider"/>.
/// </summary>
public class DefaultUnauthorizedComponentProvider : IUnauthorizedComponentProvider
{
    /// <summary>
    /// Returns a <see cref="RenderFragment"/> that displays nothing.
    /// </summary>
    public RenderFragment GetUnauthorizedComponent()
    {
        return builder =>
        {
            builder.OpenComponent<Unauthorized>(0);
            builder.CloseComponent();
        };
    }
}