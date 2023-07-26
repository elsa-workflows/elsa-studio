using Elsa.Studio.Workflows.Designer.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.Designer.Extensions;

/// <summary>
/// Contains extension methods for <see cref="IJSComponentConfiguration"/>.
/// </summary>
public static class ComponentConfigurationExtensions
{
    /// <summary>
    /// Registers custom elements.
    /// </summary>
    public static IJSComponentConfiguration RegisterCustomElements(this IJSComponentConfiguration configuration)
    {
        configuration.RegisterCustomElement<ActivityWrapper>("elsa-activity-wrapper");

        return configuration;
    }
}