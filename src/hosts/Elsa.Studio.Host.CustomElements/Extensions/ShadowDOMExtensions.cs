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
    /// Registers custom elements with Shadow DOM support using the root component mapping collection.
    /// </summary>
    /// <param name="rootComponents">The root component mapping collection.</param>
    /// <param name="enableShadowDOM">Whether to enable Shadow DOM for all components.</param>
    /// <returns>The mapping collection for chaining.</returns>
    public static RootComponentMappingCollection RegisterElsaStudioElementsWithShadowDOM(
        this RootComponentMappingCollection rootComponents, 
        bool enableShadowDOM = true)
    {
        var suffix = enableShadowDOM ? "-shadow" : "";
        
        rootComponents.Add(typeof(BackendProvider), $"elsa-backend-provider{suffix}");
        rootComponents.Add(typeof(WorkflowDefinitionEditorWrapper), $"elsa-workflow-definition-editor{suffix}");
        rootComponents.Add(typeof(WorkflowInstanceViewerWrapper), $"elsa-workflow-instance-viewer{suffix}");
        rootComponents.Add(typeof(WorkflowInstanceListWrapper), $"elsa-workflow-instance-list{suffix}");
        rootComponents.Add(typeof(WorkflowDefinitionListWrapper), $"elsa-workflow-definition-list{suffix}");
        
        return rootComponents;
    }
    
    /// <summary>
    /// Extension method to use the configuration-based registration approach.
    /// </summary>
    /// <param name="rootComponents">The root component mapping collection.</param>
    /// <param name="configuration">The configuration object to read Shadow DOM settings from.</param>
    /// <returns>The mapping collection for chaining.</returns>
    public static RootComponentMappingCollection RegisterElsaStudioElementsConditionally(
        this RootComponentMappingCollection rootComponents,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var enableShadowDOM = configuration.GetValue<bool>("ShadowDOM:Enabled", false);
        return rootComponents.RegisterElsaStudioElementsWithShadowDOM(enableShadowDOM);
    }
}