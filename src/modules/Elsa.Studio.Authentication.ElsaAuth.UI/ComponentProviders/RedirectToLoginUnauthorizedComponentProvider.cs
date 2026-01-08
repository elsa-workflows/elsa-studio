using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Authentication.ElsaAuth.UI.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.ElsaAuth.UI.ComponentProviders;

/// <inheritdoc />
public class RedirectToLoginUnauthorizedComponentProvider : IUnauthorizedComponentProvider
{
    /// <inheritdoc />
    public RenderFragment GetUnauthorizedComponent() => builder => builder.CreateComponent<RedirectToLogin>();
}

