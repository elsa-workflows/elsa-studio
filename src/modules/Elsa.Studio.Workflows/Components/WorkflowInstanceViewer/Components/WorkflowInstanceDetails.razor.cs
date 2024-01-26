using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Displays details about a workflow instance.
/// </summary>
public partial class WorkflowInstanceDetails
{
    private WorkflowInstance? _workflowInstance;
    private ActivityExecutionContextState? _workflowActivityExecutionContext;

    /// <summary>
    /// Gets or sets the workflow instance to display.
    /// </summary>
    [Parameter]
    public WorkflowInstance? WorkflowInstance { get; set; }

    /// <summary>
    /// Gets or sets the workflow definition associated with the workflow instance.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the current selected sub-workflow.
    /// </summary>
    [Parameter]
    public JsonObject? SelectedSubWorkflow { get; set; } = default!;

    [Inject] private IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;

    private IDictionary<string, StorageDriverDescriptor> StorageDriverLookup { get; set; } =
        new Dictionary<string, StorageDriverDescriptor>();

    private IWorkflowInstanceObserver WorkflowInstanceObserver { get; set; } = default!;

    private IDictionary<string, DataPanelItem> WorkflowInstanceData
    {
        get
        {
            if (_workflowInstance == null)
                return new Dictionary<string, DataPanelItem>();

            return new Dictionary<string, DataPanelItem>
            {
                ["ID"] = new(_workflowInstance.Id),
                ["Definition ID"] = new(_workflowInstance.DefinitionId, $"/workflows/definitions/{_workflowInstance.DefinitionId}/edit"),
                ["Definition version"] = new(_workflowInstance.Version.ToString()),
                ["Definition version ID"] = new(_workflowInstance.DefinitionVersionId),
                ["Correlation ID"] = new(_workflowInstance.CorrelationId),
                ["Incident Strategy"] = new(GetIncidentStrategyDisplayName(WorkflowDefinition?.Options.IncidentStrategyType)),
                ["Status"] = new(_workflowInstance.Status.ToString()),
                ["Sub status"] = new(_workflowInstance.SubStatus.ToString()),
                ["Incidents"] = new(_workflowInstance.IncidentCount.ToString()),
                ["Created"] = new(_workflowInstance.CreatedAt.ToString("G")),
                ["Updated"] = new(_workflowInstance.UpdatedAt.ToString("G")),
                ["Finished"] = new(_workflowInstance.FinishedAt?.ToString("G")),
            };
        }
    }

    private Dictionary<string, DataPanelItem> WorkflowInstanceSubWorkflowData
    {
        get
        {
            if (SelectedSubWorkflow == null)
                return new ();

            var typeName = SelectedSubWorkflow.GetTypeName();
            var version = SelectedSubWorkflow.GetVersion();
            var descriptor = ActivityRegistry.Find(typeName, version);
            var isWorkflowActivity = descriptor != null && descriptor.CustomProperties.TryGetValue("RootType", out var rootTypeNameElement) && ((JsonElement)rootTypeNameElement).GetString() == "WorkflowDefinitionActivity";
            var workflowDefinitionId = isWorkflowActivity ? SelectedSubWorkflow.GetWorkflowDefinitionId() : default;

            if (workflowDefinitionId == null)
                return new ();

            return new()
            {
                ["ID"] = new(SelectedSubWorkflow.GetId()),
                ["Name"] = new(SelectedSubWorkflow.GetName()),
                ["Type"] = new(SelectedSubWorkflow.GetTypeName()),
                ["Definition ID"] = new(workflowDefinitionId, $"/workflows/definitions/{workflowDefinitionId}/edit"),
                ["Definition version"] = new(SelectedSubWorkflow.GetVersion().ToString()),
            };
        }
    }

    public void UpdateSubWorkflow(JsonObject? obj)
    {
        SelectedSubWorkflow = obj;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var drivers = await StorageDriverService.GetStorageDriversAsync();
        StorageDriverLookup = drivers.ToDictionary(x => x.TypeName);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (_workflowInstance != WorkflowInstance)
        {
            _workflowInstance = WorkflowInstance;

            if (_workflowInstance != null)
            {
                _workflowActivityExecutionContext = GetWorkflowActivityExecutionContext(_workflowInstance);

                // If the workflow instance is still running, observe it.
                if (_workflowInstance?.Status == WorkflowStatus.Running)
                    await ObserveWorkflowInstanceAsync();
            }
        }
    }

    private async Task ObserveWorkflowInstanceAsync()
    {
        WorkflowInstanceObserver = await WorkflowInstanceObserverFactory.CreateAsync(_workflowInstance!.Id);
        WorkflowInstanceObserver.WorkflowInstanceUpdated += async _ => await InvokeAsync(async () =>
        {
            _workflowInstance = await WorkflowInstanceService.GetAsync(_workflowInstance!.Id);
            _workflowActivityExecutionContext = GetWorkflowActivityExecutionContext(_workflowInstance!);
            StateHasChanged();
        });
    }

    private ActivityExecutionContextState? GetWorkflowActivityExecutionContext(WorkflowInstance workflowInstance)
    {
        return workflowInstance.WorkflowState.ActivityExecutionContexts.FirstOrDefault(x => x.ParentContextId == null);
    }

    private string GetStorageDriverDisplayName(string? storageDriverTypeName)
    {
        if (storageDriverTypeName == null)
            return "None";

        return !StorageDriverLookup.TryGetValue(storageDriverTypeName, out var descriptor)
            ? storageDriverTypeName
            : descriptor.DisplayName;
    }

    private string GetVariableValue(Variable variable)
    {
        // TODO: Implement a REST API that returns values from the various storage providers, instead of hardcoding it here with hardcoded support for workflow storage only.
        var defaultValue = variable.Value?.ToString() ?? string.Empty;

        if (_workflowActivityExecutionContext == null)
            return defaultValue;

        if (!_workflowActivityExecutionContext.Properties.TryGetValue("PersistentVariablesDictionary",
                out var variablesDictionaryObject))
            return defaultValue;

        var dictionary = ((JsonElement)variablesDictionaryObject).Deserialize<IDictionary<string, object>>()!;
        var key = variable.Id;
        return dictionary.TryGetValue(key, out var value) ? value.ToString() ?? string.Empty : defaultValue;
    }

    private static string GetIncidentStrategyDisplayName(string? incidentStrategyTypeName)
    {
        return incidentStrategyTypeName
            ?.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).First()
            .Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Last()
            .Replace("Strategy", "")
            .Humanize() ?? "Default";
    }
}