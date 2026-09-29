using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels;
using Elsa.Studio.Labels.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.WorkflowContexts.Widgets;

/// <summary>
/// A widget that renders the workflow context editor.
/// </summary>
public class WorkflowDefinitionLabelsEditorWidget : IWidget
{
    /// <inheritdoc />
    public string Zone => "workflow-definition-properties";

    /// <inheritdoc />
    public double Order => 25;

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
    {
        // The editor lists the definition's labels as soon as it initializes, so it is not rendered at all without access.
        builder.OpenComponent<PermissionView>(0);
        builder.AddAttribute(1, nameof(PermissionView.Resource), LabelPermissions.WorkflowDefinitionLabels);
        builder.AddAttribute(2, nameof(PermissionView.Verb), PermissionVerbs.View);
        builder.AddAttribute(3, nameof(PermissionView.ChildContent), (RenderFragment)(editor =>
        {
            editor.OpenComponent<WorkflowDefinitionLabelsEditor>(0);
            editor.AddAttribute(1, nameof(WorkflowDefinitionLabelsEditor.WorkflowDefinition), attributes["WorkflowDefinition"]);
            editor.AddAttribute(2, nameof(WorkflowDefinitionLabelsEditor.WorkflowDefinitionUpdated), attributes["WorkflowDefinitionUpdated"]);
            editor.CloseComponent();
        }));
        builder.CloseComponent();
    };
}