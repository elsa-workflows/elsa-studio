using Elsa.Studio.Host.CustomElements.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Host.CustomElements.Extensions;

/// <summary>
/// Extension methods for registering custom elements with Shadow DOM support.
/// </summary>
public static class ShadowDOMExtensions
{
    /// <summary>
    /// Registers a custom element with optional Shadow DOM support.
    /// </summary>
    /// <typeparam name="TComponent">The component type to register.</typeparam>
    /// <param name="configuration">The JS component configuration.</param>
    /// <param name="tagName">The custom element tag name.</param>
    /// <param name="enableShadowDOM">Whether to enable Shadow DOM for this component.</param>
    /// <returns>The configuration for chaining.</returns>
    public static IJSComponentConfiguration RegisterCustomElementWithShadowDOM<TComponent>(
        this IJSComponentConfiguration configuration, 
        string tagName, 
        bool enableShadowDOM = true) where TComponent : class
    {
        if (enableShadowDOM)
        {
            configuration.RegisterForJavaScript(typeof(TComponent), tagName, "registerBlazorCustomElementWithShadowDOM");
        }
        else
        {
            configuration.RegisterForJavaScript(typeof(TComponent), tagName, "registerBlazorCustomElement");
        }
        
        return configuration;
    }
    
    /// <summary>
    /// Registers multiple custom elements with Shadow DOM support.
    /// </summary>
    /// <param name="mapping">The root component mapping collection.</param>
    /// <param name="enableShadowDOM">Whether to enable Shadow DOM for all components.</param>
    /// <returns>The mapping collection for chaining.</returns>
    public static RootComponentMappingCollection RegisterElsaStudioElementsWithShadowDOM(
        this RootComponentMappingCollection mapping, 
        bool enableShadowDOM = true)
    {
        var suffix = enableShadowDOM ? "-shadow" : "";
        
        mapping.Add(typeof(BackendProvider), $"elsa-backend-provider{suffix}");
        mapping.Add(typeof(WorkflowDefinitionEditorWrapper), $"elsa-workflow-definition-editor{suffix}");
        mapping.Add(typeof(WorkflowInstanceViewerWrapper), $"elsa-workflow-instance-viewer{suffix}");
        mapping.Add(typeof(WorkflowInstanceListWrapper), $"elsa-workflow-instance-list{suffix}");
        mapping.Add(typeof(WorkflowDefinitionListWrapper), $"elsa-workflow-definition-list{suffix}");
        
        return mapping;
    }
}