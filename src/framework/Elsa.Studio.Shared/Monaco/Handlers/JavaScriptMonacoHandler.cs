using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.JSInterop;

namespace Elsa.Studio.Monaco.Handlers;

/// <summary>
/// Handles Monaco editor for JavaScript.
/// </summary>
public class JavaScriptMonacoHandler(IJSRuntime jsRuntime, TypeDefinitionService typeDefinitionService)
    : IMonacoHandler
{
    /// <inheritdoc />
    public async ValueTask InitializeAsync(MonacoContext context)
    {
        var activityDescriptor = context.CustomProperties.TryGetValue(nameof(ActivityDescriptor), out var activityDescriptorObj) ? (ActivityDescriptor)activityDescriptorObj : null;
        var inputDescriptor = context.CustomProperties.TryGetValue(nameof(InputDescriptor), out var inputDescriptorObj) ? (InputDescriptor)inputDescriptorObj : null;
        var workflowDefinitionId = context.CustomProperties.TryGetValue("WorkflowDefinitionId", out var workflowDefinitionIdObj) ? (string)workflowDefinitionIdObj : null;
        var expressionDescriptor = context.ExpressionDescriptor;
        
        if(expressionDescriptor.Type != "JavaScript" && activityDescriptor?.TypeName != "Elsa.RunJavaScript")
            return;
        
        var activityTypeName = activityDescriptor?.TypeName;
        var propertyName = inputDescriptor?.Name;
        
        if (activityTypeName == null || propertyName == null || workflowDefinitionId == null)
            return;
        
        var data = await typeDefinitionService.GetTypeDefinition(workflowDefinitionId, activityTypeName, propertyName);
        await jsRuntime.InvokeVoidAsync("monaco.languages.typescript.javascriptDefaults.addExtraLib", data, null);
    }
}