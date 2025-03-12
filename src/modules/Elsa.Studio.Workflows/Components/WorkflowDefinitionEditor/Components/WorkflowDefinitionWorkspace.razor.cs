using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// A workspace for editing a workflow definition.
public partial class WorkflowDefinitionWorkspace : IWorkspace
{
    private MudDynamicTabs _dynamicTabs = null!;
    private WorkflowDefinition? _workflowDefinition = null!;
    private WorkflowDefinition? _selectedWorkflowDefinition = null!;
    
    /// Gets or sets the workflow definition to edit.
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    
    /// Gets or sets a specific version of the workflow definition to view.
    [Parameter] public WorkflowDefinition SelectedWorkflowDefinition { get; set; } = null!;

    /// <summary>An event that is invoked when a workflow definition has been executed.</summary>
    /// <remarks>The ID of the workflow instance is provided as the value to the event callback.</remarks>
    [Parameter] public Func<string, Task>? WorkflowDefinitionExecuted { get; set; }
    
    /// Gets or sets the event that occurs when the workflow definition version is updated.
    [Parameter] public Func<WorkflowDefinition, Task>? WorkflowDefinitionVersionSelected { get; set; }
    
    /// Gets or sets the event that occurs when an activity is selected.
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    /// An event that is invoked when the workflow definition is updated.
    public event Func<Task>? WorkflowDefinitionUpdated;

    /// <inheritdoc />
    public bool IsReadOnly => _selectedWorkflowDefinition.GetIsReadOnly();

    /// <inheritdoc />
    public bool HasWorkflowEditPermission => (_selectedWorkflowDefinition?.Links?.Count(l => l.Rel == "publish") ?? 0) > 0;

    /// Gets the selected activity ID.
    public string? SelectedActivityId => WorkflowEditor.SelectedActivityId;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;

    private WorkflowEditor WorkflowEditor { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _workflowDefinition = WorkflowDefinition;
        _selectedWorkflowDefinition = SelectedWorkflowDefinition;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _workflowDefinition = WorkflowDefinition;
        _selectedWorkflowDefinition = SelectedWorkflowDefinition;
        
        if (_selectedWorkflowDefinition == null)
            _selectedWorkflowDefinition = _workflowDefinition;
    }
    
    /// Displays the specified workflow definition version.
    public async Task DisplayWorkflowDefinitionVersionAsync(WorkflowDefinition workflowDefinition)
    {
        if(_selectedWorkflowDefinition == workflowDefinition)
            return;
        
        _selectedWorkflowDefinition = workflowDefinition;
        
        if(workflowDefinition.IsLatest)
            _workflowDefinition = workflowDefinition;

        if (WorkflowDefinitionVersionSelected != null)
            await WorkflowDefinitionVersionSelected(_selectedWorkflowDefinition);

        StateHasChanged();
    }
    
    /// Gets the currently selected workflow definition version.
    public WorkflowDefinition? GetSelectedWorkflowDefinitionVersion() => _selectedWorkflowDefinition;
    
    /// Determines whether the workspace is currently viewing a specific version of a workflow definition.
    public bool IsSelectedDefinition(string definitionVersionId)
    {
        return _selectedWorkflowDefinition?.Id == definitionVersionId;
    }
    
    /// Gets the selected workflow definition.
    public WorkflowDefinition? GetSelectedDefinition()
    {
        return _selectedWorkflowDefinition;
    }
    
    /// Displays the latest version of a workflow definition asynchronously.
    public async Task DisplayLatestWorkflowDefinitionVersionAsync()
    {
        var definitionId = _workflowDefinition!.DefinitionId;
        var definition = (await WorkflowDefinitionService.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest))!;
        await DisplayWorkflowDefinitionVersionAsync(definition);
    }

    private async Task OnWorkflowDefinitionPropsUpdated()
    {
        await WorkflowEditor.NotifyWorkflowChangedAsync();
    }

    private async Task OnWorkflowDefinitionUpdated()
    {
        _workflowDefinition = WorkflowEditor.WorkflowDefinition!;
        _selectedWorkflowDefinition = _workflowDefinition;
        StateHasChanged();

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }
}