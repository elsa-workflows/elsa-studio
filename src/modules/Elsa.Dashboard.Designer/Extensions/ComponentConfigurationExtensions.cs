using Elsa.Dashboard.Designer.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Dashboard.Designer.Extensions;

public static class ComponentConfigurationExtensions
{
    public static IJSComponentConfiguration RegisterCustomElements(this IJSComponentConfiguration configuration)
    {
        configuration.RegisterCustomElement<ActivityWrapper>("elsa-activity-wrapper");
        configuration.RegisterCustomElement<BlazorButton>("blazor-button"); // Experiment.

        return configuration;
    }
}