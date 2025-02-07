using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.LogPersistenceStrategies;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Enums;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Shared.Serialization;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;

/// <summary>
/// Displays details about a workflow instance.
/// </summary>
public partial class WorkflowInstanceDetails
{
    private readonly JsonSerializerOptions _serializerOptions = SerializerOptions.LogPersistenceConfigSerializerOptions;

    private WorkflowInstance? _workflowInstance;

    private ActivityExecutionRecord? _workflowActivityExecutionRecord;
    private ICollection<ResolvedVariable> _variables = [];

    /// <summary>
    /// Gets or sets the workflow instance to display.
    /// </summary>
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }

    /// <summary>
    /// Gets or sets the workflow definition associated with the workflow instance.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets the current selected sub-workflow.
    /// </summary>
    [Parameter] public JsonObject? SelectedSubWorkflow { get; set; }

    /// <summary>
    /// Gets or sets the current selected sub-workflow executions.
    /// </summary>
    [Parameter] public ICollection<ActivityExecutionRecord>? SelectedSubWorkflowExecutions { get; set; }
    
    /// <summary>
    /// Gets or sets the current selected incident activity id.
    /// </summary>
    [Parameter] public Func<string, Task>? IncidentActivityIdClicked { get; set; }

    [Inject] private IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = default!;
    [Inject] private ITimeFormatter TimeFormatter { get; set; } = default!;

    private IDictionary<string, StorageDriverDescriptor> StorageDriverLookup { get; set; } = new Dictionary<string, StorageDriverDescriptor>();

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
                ["Created"] = new(TimeFormatter.Format(_workflowInstance.CreatedAt)),
                ["Updated"] = new(TimeFormatter.Format(_workflowInstance.UpdatedAt)),
                ["Finished"] = new(TimeFormatter.Format(_workflowInstance.FinishedAt)),
                ["Log Persistence Strategy"] = new(GetLogPersistenceConfigurationDisplayName(GetLogPersistenceConfiguration()?.StrategyType))
            };
        }
    }

    private LogPersistenceConfiguration? GetLogPersistenceConfiguration()
    {
        if (!WorkflowDefinition!.CustomProperties.TryGetValue("logPersistenceConfig", out var value) || value == null!)
            return null;


        var configJson = ((JsonElement)value).GetProperty("default");
        var config = JsonSerializer.Deserialize<LogPersistenceConfiguration>(configJson, _serializerOptions);
        return config;
    }

    private Dictionary<string, DataPanelItem> WorkflowVariableData
    {
        get
        {
            return WorkflowDefinition == null
                ? new Dictionary<string, DataPanelItem>()
                : WorkflowDefinition.Variables.ToDictionary(entry => entry.Name, entry => new DataPanelItem(GetVariableValue(entry)));
        }
    }

    private Dictionary<string, DataPanelItem> WorkflowInputData
    {
        get
        {
            if (_workflowInstance == null || WorkflowDefinition == null)
                return new Dictionary<string, DataPanelItem>();

            var inputData = new Dictionary<string, DataPanelItem>();
            foreach (var input in WorkflowDefinition.Inputs)
            {
                _workflowInstance.WorkflowState.Input.TryGetValue(input.Name, out object? inputFromInstance);
                var inputName = !string.IsNullOrWhiteSpace(input.DisplayName) ? input.DisplayName : input.Name;
                inputData.Add(inputName, new DataPanelItem(inputFromInstance?.ToString()));
            }

            return inputData;
        }
    }

    private Dictionary<string, DataPanelItem> WorkflowOutputData
    {
        get
        {
            if (_workflowInstance == null || WorkflowDefinition == null)
                return new Dictionary<string, DataPanelItem>();

            var outputData = new Dictionary<string, DataPanelItem>();
            foreach (var output in WorkflowDefinition.Outputs)
            {
                _workflowInstance.WorkflowState.Output.TryGetValue(output.Name, out object? outputFromInstance);
                var outputName = !string.IsNullOrWhiteSpace(output.DisplayName) ? output.DisplayName : output.Name;
                outputData.Add(outputName, new DataPanelItem(outputFromInstance?.ToString()));
            }

            return outputData;
        }
    }

    private Dictionary<string, DataPanelItem> WorkflowInstanceSubWorkflowData
    {
        get
        {
            if (SelectedSubWorkflow == null)
                return new();

            var typeName = SelectedSubWorkflow.GetTypeName();
            var version = SelectedSubWorkflow.GetVersion();
            var descriptor = ActivityRegistry.Find(typeName, version);
            var isWorkflowActivity = descriptor != null &&
                                     descriptor.CustomProperties.TryGetValue("RootType", out var rootTypeNameElement) &&
                                     ((JsonElement)rootTypeNameElement).GetString() == "WorkflowDefinitionActivity";
            var workflowDefinitionId = isWorkflowActivity ? SelectedSubWorkflow.GetWorkflowDefinitionId() : default;

            if (workflowDefinitionId == null)
                return new();

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
    
    private IEnumerable<IDictionary<string, DataPanelItem>> IncidentsData
    {
        get
        {
            if (_workflowInstance == null)
                return new List<IDictionary<string, DataPanelItem>>();

            return _workflowInstance.WorkflowState.Incidents
                .Select(i => new Dictionary<string, DataPanelItem>()
                {
                    ["ActivityId"] = new(i.ActivityId,null, () => OnIncidentActivityIdClicked(i.ActivityId)),
                    ["Message"] = new(i.Exception?.Message ?? ""),
                    ["InnerException"] = new(i.Exception?.InnerException != null
                        ? i.Exception?.InnerException.Type + ": " + i.Exception?.InnerException.Message
                        : default),
                    ["StackTrace"] = new(i.Exception?.StackTrace)
                });
        }
    }
    
    private async Task OnIncidentActivityIdClicked(string? activityId)
    {
        if (IncidentActivityIdClicked != null && !string.IsNullOrWhiteSpace(activityId))
            await IncidentActivityIdClicked(activityId);
    }

    private Dictionary<string, DataPanelItem> SubWorkflowInputData
    {
        get
        {
            if (SelectedSubWorkflowExecutions == null || SelectedSubWorkflow == null)
                return new Dictionary<string, DataPanelItem>();

            var execution = SelectedSubWorkflowExecutions.LastOrDefault();
            var inputData = new Dictionary<string, DataPanelItem>();
            var activityState = execution?.ActivityState;
            if (activityState != null)
            {
                var activityDescriptor =
                    ActivityRegistry.Find(SelectedSubWorkflow.GetTypeName(), SelectedSubWorkflow.GetVersion())!;
                foreach (var inputDescriptor in activityDescriptor.Inputs)
                {
                    var inputValue = activityState.TryGetValue(inputDescriptor.Name, out var value) ? value : default;
                    inputData[inputDescriptor.DisplayName ?? inputDescriptor.Name] = new(inputValue?.ToString());
                }
            }

            return inputData;
        }
    }

    private Dictionary<string, DataPanelItem> SubWorkflowOutputData
    {
        get
        {
            if (SelectedSubWorkflowExecutions == null || SelectedSubWorkflow == null)
                return new Dictionary<string, DataPanelItem>();

            var execution = SelectedSubWorkflowExecutions.LastOrDefault();
            var outputData = new Dictionary<string, DataPanelItem>();

            if (execution != null)
            {
                var outputs = execution.Outputs;
                var activityDescriptor =
                    ActivityRegistry.Find(SelectedSubWorkflow.GetTypeName(), SelectedSubWorkflow.GetVersion())!;
                var outputDescriptors = activityDescriptor.Outputs;

                foreach (var outputDescriptor in outputDescriptors)
                {
                    var outputValue = outputs != null
                        ? outputs.TryGetValue(outputDescriptor.Name, out var value) ? value : default
                        : default;
                    outputData[outputDescriptor.DisplayName ?? outputDescriptor.Name] = new(outputValue?.ToString());
                }
            }

            return outputData;
        }
    }

    /// Updates the selected sub-workflow.
    public async Task UpdateSubWorkflowAsync(JsonObject? obj)
    {
        SelectedSubWorkflow = obj;
        SelectedSubWorkflowExecutions = obj == null
            ? null
            : (await ActivityExecutionService.ListAsync(WorkflowInstance!.Id, obj.GetNodeId())).ToList();
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
                await GetWorkflowActivityExecutionRecordAsync(_workflowInstance.Id);
                await GetVariablesAsync(_workflowInstance.Id);
            }
        }
    }

    private async Task GetWorkflowActivityExecutionRecordAsync(string workflowInstanceId)
    {
        _workflowActivityExecutionRecord = await GetLastWorkflowActivityExecutionRecordAsync(workflowInstanceId);
    }

    private async Task GetVariablesAsync(string workflowInstanceId)
    {
        _variables = (await WorkflowInstanceService.GetVariablesAsync(workflowInstanceId)).ToList();
    }

    private async Task<ActivityExecutionRecord?> GetLastWorkflowActivityExecutionRecordAsync(string workflowInstanceId)
    {
        var rootWorkflowActivityExecutionContext = WorkflowInstance?.WorkflowState.ActivityExecutionContexts.FirstOrDefault(x => x.ParentContextId == null);
        if (rootWorkflowActivityExecutionContext == null) return null;
        var rootWorkflowActivityNodeId = rootWorkflowActivityExecutionContext.ScheduledActivityNodeId;
        var records = await ActivityExecutionService.ListAsync(workflowInstanceId, rootWorkflowActivityNodeId);
        return records.MaxBy(x => x.StartedAt);
    }

    private string GetStorageDriverDisplayName(string? storageDriverTypeName)
    {
        if (storageDriverTypeName == null)
            return "None";

        return !StorageDriverLookup.TryGetValue(storageDriverTypeName, out var descriptor)
            ? storageDriverTypeName
            : descriptor.DisplayName;
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(JsonSerializerOptions)")]
    private string GetVariableValue(Variable variable)
    {
        var resolvedVariable = _variables.FirstOrDefault(x => x.Id == variable.Id);
        return resolvedVariable?.Value?.ToString() ?? string.Empty;
    }

    private static string GetIncidentStrategyDisplayName(string? incidentStrategyTypeName)
    {
        return incidentStrategyTypeName
            ?.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).First()
            .Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Last()
            .Replace("Strategy", "")
            .Humanize() ?? "Default";
    }

    private static string? GetLogPersistenceConfigurationDisplayName(string? logPersistenceConfigurationTypeName)
    {
        //"Elsa.Workflows.LogPersistence.Strategies.Include, Elsa.Workflows.Core"
        return logPersistenceConfigurationTypeName?.Split(",")[0].Split(".")[^1] ;
    }
}