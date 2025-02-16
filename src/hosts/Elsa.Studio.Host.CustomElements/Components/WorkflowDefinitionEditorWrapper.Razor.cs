using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Host.CustomElements.Components;

public partial class WorkflowDefinitionEditorWrapper
{
    /// Provides a wrapper component for managing and interacting with a workflow definition editor.
    public WorkflowDefinitionEditorWrapper()
    {
        WorkflowDefinitionExecuted = async e => await OnWorkflowDefinitionExecuted.InvokeAsync(e);
    }
    
    [Parameter]
    public string Name { get;set; } = "Alice";

    [Parameter]
    public EventCallback OnClicked { get; set; }

    /// The ID of the workflow definition to edit.
    [Parameter] public string DefinitionId { get; set; } = null!;

    /// <summary>An event that is invoked when a workflow definition has been executed.</summary>
    /// <remarks>The ID of the workflow instance is provided as the value to the event callback.</remarks>
    [Parameter] public Func<string, Task>? WorkflowDefinitionExecuted { get; set; }

    /// Represents an event invoked when a workflow definition is executed.
    /// The event passes the ID of the executed workflow instance as its argument.
    [Parameter] public EventCallback<string> OnWorkflowDefinitionExecuted { get; set; }

    /// Gets or sets the event that occurs when the workflow definition version is updated.
    [Parameter] public Func<WorkflowDefinition, Task>? WorkflowDefinitionVersionSelected { get; set; }

    /// Gets or sets the event triggered when an activity is selected.
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    /// Gets the currently selected activity ID.
    public string? SelectedActivityId => WorkflowDefinitionEditor.SelectedActivityId;

    private WorkflowDefinitionEditor WorkflowDefinitionEditor { get; set; } = null!;

    /// Gets the currently selected workflow definition version.
    public WorkflowDefinition? GetSelectedWorkflowDefinitionVersion() => WorkflowDefinitionEditor.GetSelectedWorkflowDefinitionVersion();
}