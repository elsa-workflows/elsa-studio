using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// <summary>
/// A workspace for editing a workflow definition.
/// </summary>
public partial class WorkflowDefinitionWorkspace : IWorkspace
{
    private MudDynamicTabs _dynamicTabs = default!;

    /// <summary>
    /// Gets or sets the workflow definition to edit.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;

    /// <summary>
    /// Gets or sets a specific version of the workflow definition to view.
    /// </summary>
    [Parameter] public WorkflowDefinition SelectedWorkflowDefinition { get; set; } = default!;

    /// <summary>An event that is invoked when a workflow definition has been executed.</summary>
    /// <remarks>The ID of the workflow instance is provided as the value to the event callback.</remarks>
    [Parameter] public EventCallback<string> WorkflowDefinitionExecuted { get; set; }

    /// <summary>
    /// Gets or sets the event that occurs when the workflow definition version is updated.
    /// </summary>
    [Parameter] public EventCallback<WorkflowDefinition> WorkflowDefinitionVersionSelected { get; set; }

    /// <summary>
    /// Gets or sets the event that occurs when an activity is selected.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// An event that is invoked when the workflow definition is updated.
    public event Func<Task>? WorkflowDefinitionUpdated;

    /// <inheritdoc />
    public bool IsReadOnly => SelectedWorkflowDefinition?.IsLatest == false || (SelectedWorkflowDefinition?.Links?.Count(l => l.Rel == "publish") ?? 0) == 0;

    /// <inheritdoc />
    public bool HasWorkflowEditPermission => (SelectedWorkflowDefinition?.Links?.Count(l => l.Rel == "publish") ?? 0) > 0;

    /// Gets the selected activity ID.
    public string? SelectedActivityId => WorkflowEditor.SelectedActivityId;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private WorkflowEditor WorkflowEditor { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (SelectedWorkflowDefinition == null!)
            SelectedWorkflowDefinition = WorkflowDefinition;
    }

    /// <summary>
    /// Displays the specified workflow definition version.
    /// </summary>
    public void DisplayWorkflowDefinitionVersion(WorkflowDefinition workflowDefinition)
    {
        SelectedWorkflowDefinition = workflowDefinition;

        if (WorkflowDefinitionVersionSelected.HasDelegate)
            WorkflowDefinitionVersionSelected.InvokeAsync(SelectedWorkflowDefinition);

        StateHasChanged();
    }

    /// <summary>
    /// Gets the currently selected workflow definition version.
    /// </summary>
    public WorkflowDefinition? GetSelectedWorkflowDefinitionVersion() => SelectedWorkflowDefinition;

    /// <summary>
    /// Refreshes the active workflow definition.
    /// </summary>
    public async Task RefreshActiveWorkflowAsync()
    {
        var definitionId = WorkflowDefinition.DefinitionId;
        var definition = await WorkflowDefinitionService.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest);
        SelectedWorkflowDefinition = definition!;
        StateHasChanged();
    }

    private async Task OnWorkflowDefinitionPropsUpdated()
    {
        await WorkflowEditor.NotifyWorkflowChangedAsync();
    }

    private async Task OnWorkflowDefinitionUpdated()
    {
        SelectedWorkflowDefinition = WorkflowEditor.WorkflowDefinition!;
        StateHasChanged();

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }
}