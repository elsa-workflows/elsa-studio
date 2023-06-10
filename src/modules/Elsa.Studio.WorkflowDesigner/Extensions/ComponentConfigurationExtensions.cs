using Elsa.Studio.WorkflowDesigner.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.WorkflowDesigner.Extensions;

public static class ComponentConfigurationExtensions
{
    public static IJSComponentConfiguration RegisterCustomElements(this IJSComponentConfiguration configuration)
    {
        configuration.RegisterCustomElement<ActivityWrapper>("elsa-activity-wrapper");

        return configuration;
    }
}