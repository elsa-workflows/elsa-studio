using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Screens.EditWorkflowDefinition.Components;

public partial class Workspace : IWorkspace
{
    private MudDynamicTabs _dynamicTabs = default!;

    [Parameter] public IList<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    [Parameter] public WorkflowDefinition? SelectedWorkflowDefinitionVersion { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
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

    public async Task RefreshActiveWorkflowAsync()
    {
        var selectedWorkflowDefinition = SelectedWorkflowDefinition;
        
        if(selectedWorkflowDefinition == null)
            return;

        var definitionId = selectedWorkflowDefinition.DefinitionId;
        var definition = await WorkflowDefinitionService.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest);
        WorkflowDefinitions[ActiveTabIndex] = definition!;
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