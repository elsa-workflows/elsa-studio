using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.JSInterop;

namespace Elsa.Studio.MonacoHandlers;

/// <summary>
/// Handles Monaco editor for JavaScript.
/// </summary>
public class JavaScriptMonacoHandler(IJSRuntime jsRuntime, TypeDefinitionService typeDefinitionService)
    : IMonacoHandler
{
    /// <inheritdoc />
    public async ValueTask InitializeAsync(MonacoContext context)
    {
        var editorContext = context.DisplayInputEditorContext;
        var activityDescriptor = editorContext.ActivityDescriptor;
        var expressionDescriptor = editorContext.SelectedExpressionDescriptor;
        
        if(expressionDescriptor?.Type != "JavaScript" && activityDescriptor.TypeName != "Elsa.RunJavaScript")
            return;
        
        var activityTypeName = editorContext.ActivityDescriptor.TypeName;
        var propertyName = editorContext.InputDescriptor.Name;
        var definitionId = editorContext.WorkflowDefinition.DefinitionId;
        var data = await typeDefinitionService.GetTypeDefinition(definitionId, activityTypeName, propertyName);
        await jsRuntime.InvokeVoidAsync("monaco.languages.typescript.javascriptDefaults.addExtraLib", data, null);
    }
}