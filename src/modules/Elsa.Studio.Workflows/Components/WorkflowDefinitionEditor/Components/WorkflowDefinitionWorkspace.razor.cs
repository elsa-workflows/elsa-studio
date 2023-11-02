using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
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
    [Parameter] public WorkflowDefinition? SelectedWorkflowDefinitionVersion { get; set; }
    
    /// <summary>
    /// An event that is invoked when the workflow definition is updated.
    /// </summary>
    public event Func<Task>? WorkflowDefinitionUpdated;

    /// <inheritdoc />
    public bool IsReadOnly => SelectedWorkflowDefinitionVersion?.IsLatest == false;
    
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    
    private WorkflowEditor WorkflowEditor { get; set; } = default!;

    /// <summary>
    /// Displays the specified workflow definition version.
    /// </summary>
    public void DisplayWorkflowDefinitionVersion(WorkflowDefinition workflowDefinition)
    {
        SelectedWorkflowDefinitionVersion = workflowDefinition;
        StateHasChanged();
    }

    /// <summary>
    /// Displays the latest workflow definition version, which enables editing.
    /// </summary>
    public void ResumeEditing()
    {
        SelectedWorkflowDefinitionVersion = default;
        StateHasChanged();
    }

    /// <summary>
    /// Refreshes the active workflow definition.
    /// </summary>
    public async Task RefreshActiveWorkflowAsync()
    {
        var definitionId = WorkflowDefinition.DefinitionId;
        var definition = await WorkflowDefinitionService.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest);
        WorkflowDefinition = definition!;
        StateHasChanged();
    }
    
    private Task AddTabCallback()
    {
        return Task.CompletedTask;
    }

    private Task CloseTabCallback(MudTabPanel arg)
    {
        return Task.CompletedTask;
    }

    private async Task OnWorkflowDefinitionPropsUpdated()
    {
        await WorkflowEditor.NotifyWorkflowChangedAsync();
    }

    private async Task OnWorkflowDefinitionUpdated()
    {
        WorkflowDefinition = WorkflowEditor.WorkflowDefinition!;
        StateHasChanged();

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }
}