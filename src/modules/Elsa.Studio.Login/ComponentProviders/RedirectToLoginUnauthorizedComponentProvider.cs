using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.ComponentProviders;

/// <inheritdoc />
public class RedirectToLoginUnauthorizedComponentProvider : IUnauthorizedComponentProvider
{
    /// <inheritdoc />
    public RenderFragment GetUnauthorizedComponent()
    {
        return builder => builder.CreateComponent<RedirectToLogin>();
    }
}