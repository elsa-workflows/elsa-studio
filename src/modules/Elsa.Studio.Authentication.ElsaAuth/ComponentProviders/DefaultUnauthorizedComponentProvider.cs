using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.ElsaAuth.ComponentProviders;

/// <summary>
/// A safe default unauthorized component provider for ElsaAuth (renders the generic Unauthorized component).
/// Hosts can override this registration to provide custom unauthorized UX.
/// </summary>
public class DefaultUnauthorizedComponentProvider : IUnauthorizedComponentProvider
{
    /// <inheritdoc />
    public RenderFragment GetUnauthorizedComponent() => builder =>
    {
        builder.OpenComponent<Unauthorized>(0);
        builder.CloseComponent();
    };
}

