using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit;

public partial class Workspace : IWorkspace
{
    private MudDynamicTabs _dynamicTabs = default!;

    [Parameter] public IList<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    [Parameter] public WorkflowDefinition? SelectedWorkflowDefinitionVersion { get; set; }
    public event Func<Task>? WorkflowDefinitionUpdated;
    public bool IsReadOnly => SelectedWorkflowDefinitionVersion?.IsLatest == false;

    private int ActiveTabIndex { get; } = 0;
    private IDictionary<string, WorkflowEditor> WorkflowEditors { get; } = new Dictionary<string, WorkflowEditor>();
    private WorkflowDefinition? SelectedWorkflowDefinition => ActiveTabIndex >= 0 && ActiveTabIndex < WorkflowDefinitions.Count ? WorkflowDefinitions.ElementAtOrDefault(ActiveTabIndex) : default;

    public void DisplayWorkflowDefinitionVersion(WorkflowDefinition workflowDefinition)
    {
        SelectedWorkflowDefinitionVersion = workflowDefinition;
        StateHasChanged();
    }

    public void ResumeEditing()
    {
        SelectedWorkflowDefinitionVersion = default;
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
        var workflowEditor = WorkflowEditors[SelectedWorkflowDefinition!.DefinitionId];
        await workflowEditor.NotifyWorkflowChangedAsync();
    }

    private async Task OnWorkflowDefinitionUpdated()
    {
        var definitionId = SelectedWorkflowDefinition!.DefinitionId;
        var workflowEditor = WorkflowEditors[definitionId];
        WorkflowDefinitions[ActiveTabIndex] = workflowEditor.WorkflowDefinition!;
        StateHasChanged();

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }
}