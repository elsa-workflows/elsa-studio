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

    [Inject] private IStorageDriverService StorageDriverService { get; set; } = null!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityExecutionService ActivityExecutionService { get; set; } = null!;
    [Inject] private ITimeFormatter TimeFormatter { get; set; } = null!;

    private IDictionary<string, StorageDriverDescriptor> StorageDriverLookup { get; set; } = new Dictionary<string, StorageDriverDescriptor>();

    private DataPanelModel WorkflowInstanceData
    {
        get
        {
            if (_workflowInstance == null)
                return new();

            return new()
            {
                new DataPanelItem("ID", _workflowInstance.Id),
                new DataPanelItem("Definition ID", _workflowInstance.DefinitionId, $"/workflows/definitions/{_workflowInstance.DefinitionId}/edit"),
                new DataPanelItem("Definition version", _workflowInstance.Version.ToString()),
                new DataPanelItem("Definition version ID", _workflowInstance.DefinitionVersionId),
                new DataPanelItem("Correlation ID", _workflowInstance.CorrelationId),
                new DataPanelItem("Incident Strategy", GetIncidentStrategyDisplayName(WorkflowDefinition?.Options.IncidentStrategyType)),
                new DataPanelItem("Status", _workflowInstance.Status.ToString()),
                new DataPanelItem("Sub status", _workflowInstance.SubStatus.ToString()),
                new DataPanelItem("Incidents", _workflowInstance.IncidentCount.ToString()),
                new DataPanelItem("Created", TimeFormatter.Format(_workflowInstance.CreatedAt)),
                new DataPanelItem("Updated", TimeFormatter.Format(_workflowInstance.UpdatedAt)),
                new DataPanelItem("Finished", TimeFormatter.Format(_workflowInstance.FinishedAt)),
                new DataPanelItem("Log Persistence Strategy", GetLogPersistenceConfigurationDisplayName(GetLogPersistenceConfiguration()?.StrategyType))
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

    private DataPanelModel WorkflowVariableData
    {
        get
        {
            return WorkflowDefinition == null
                ? new DataPanelModel()
                : new DataPanelModel(WorkflowDefinition.Variables.Select(entry => new DataPanelItem(entry.Name, GetVariableValue(entry))));
        }
    }

    private DataPanelModel WorkflowInputData
    {
        get
        {
            if (_workflowInstance == null || WorkflowDefinition == null)
                return new();

            var inputData = new DataPanelModel();
            foreach (var input in WorkflowDefinition.Inputs)
            {
                _workflowInstance.WorkflowState.Input.TryGetValue(input.Name, out object? inputFromInstance);
                var inputName = !string.IsNullOrWhiteSpace(input.DisplayName) ? input.DisplayName : input.Name;
                inputData.Add(inputName, inputFromInstance?.ToString());
            }

            return inputData;
        }
    }

    private DataPanelModel WorkflowOutputData
    {
        get
        {
            if (_workflowInstance == null || WorkflowDefinition == null)
                return new DataPanelModel();

            var outputData = new DataPanelModel();
            foreach (var output in WorkflowDefinition.Outputs)
            {
                _workflowInstance.WorkflowState.Output.TryGetValue(output.Name, out object? outputFromInstance);
                var outputName = !string.IsNullOrWhiteSpace(output.DisplayName) ? output.DisplayName : output.Name;
                outputData.Add(outputName, outputFromInstance?.ToString());
            }

            return outputData;
        }
    }

    private DataPanelModel WorkflowInstanceSubWorkflowData
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
            var workflowDefinitionId = isWorkflowActivity ? SelectedSubWorkflow.GetWorkflowDefinitionId() : null;

            if (workflowDefinitionId == null)
                return new();

            return new()
            {
                new DataPanelItem("ID", SelectedSubWorkflow.GetId()),
                new DataPanelItem("Name", SelectedSubWorkflow.GetName()),
                new DataPanelItem("Type", SelectedSubWorkflow.GetTypeName()),
                new DataPanelItem("Definition ID", workflowDefinitionId, $"/workflows/definitions/{workflowDefinitionId}/edit"),
                new DataPanelItem("Definition version", SelectedSubWorkflow.GetVersion().ToString()),
            };
        }
    }

    private DataPanelModel SubWorkflowInputData
    {
        get
        {
            if (SelectedSubWorkflowExecutions == null || SelectedSubWorkflow == null)
                return new();

            var execution = SelectedSubWorkflowExecutions.LastOrDefault();
            var inputData = new DataPanelModel();
            var activityState = execution?.ActivityState;
            if (activityState != null)
            {
                var activityDescriptor = ActivityRegistry.Find(SelectedSubWorkflow.GetTypeName(), SelectedSubWorkflow.GetVersion())!;
                foreach (var inputDescriptor in activityDescriptor.Inputs)
                {
                    var inputValue = activityState.TryGetValue(inputDescriptor.Name, out var value) ? value : null;
                    inputData.Add(inputDescriptor.DisplayName ?? inputDescriptor.Name, inputValue?.ToString());
                }
            }

            return inputData;
        }
    }

    private DataPanelModel SubWorkflowOutputData
    {
        get
        {
            if (SelectedSubWorkflowExecutions == null || SelectedSubWorkflow == null)
                return new();

            var execution = SelectedSubWorkflowExecutions.LastOrDefault();
            var outputData = new DataPanelModel();

            if (execution != null)
            {
                var outputs = execution.Outputs;
                var activityDescriptor =
                    ActivityRegistry.Find(SelectedSubWorkflow.GetTypeName(), SelectedSubWorkflow.GetVersion())!;
                var outputDescriptors = activityDescriptor.Outputs;

                foreach (var outputDescriptor in outputDescriptors)
                {
                    var outputValue = outputs != null
                        ? outputs.TryGetValue(outputDescriptor.Name, out var value) ? value : null
                        : null;
                    outputData.Add(outputDescriptor.DisplayName ?? outputDescriptor.Name, outputValue?.ToString());
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