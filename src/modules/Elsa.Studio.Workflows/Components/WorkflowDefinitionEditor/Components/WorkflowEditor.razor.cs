using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Radzen;
using Radzen.Blazor;
using Refit;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// <summary>
/// A component that allows the user to edit a workflow definition.
/// </summary>
public partial class WorkflowEditor
{
    private readonly RateLimitedFunc<bool, Task> _rateLimitedSaveChangesAsync;
    private bool _autoSave = true;
    private bool _isDirty;
    private bool _isProgressing;
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private int _activityPropertiesPaneHeight = 300;
    private DiagramDesignerWrapper _diagramDesigner = default!;

    /// <inheritdoc />
    public WorkflowEditor()
    {
        _rateLimitedSaveChangesAsync = Debouncer.Debounce<bool, Task>(async readDiagram => await SaveChangesAsync(readDiagram, false, false), TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Gets or sets the drag and drop manager via property injection.
    /// </summary>
    [CascadingParameter]
    public DragDropManager DragDropManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter]
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets a callback that is invoked when the workflow definition is updated.
    /// </summary>
    [Parameter]
    public Func<Task>? WorkflowDefinitionUpdated { get; set; }

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private string? SelectedActivityId { get; set; }
    private ActivityProperties.ActivityProperties? ActivityPropertiesTab { get; set; }

    private RadzenSplitterPane ActivityPropertiesPane
    {
        get => _activityPropertiesPane;
        set
        {
            _activityPropertiesPane = value;

            // Prefix the ID with a non-numerical value so it can always be used as a query selector (sometimes, Radzen generates a unique ID starting with a number).
            _activityPropertiesPane.UniqueID = $"pane-{value.UniqueID}";
        }
    }

    /// <summary>
    /// Gets or sets a flag indicating whether the workflow definition is dirty.
    /// </summary>
    public async Task NotifyWorkflowChangedAsync()
    {
        await HandleChangesAsync(false);
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ActivityRegistry.EnsureLoadedAsync();

        if (WorkflowDefinition?.Root == null)
            return;

        SelectActivity(WorkflowDefinition.Root);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await UpdateActivityPropertiesVisibleHeightAsync();
    }

    private async Task HandleChangesAsync(bool readDiagram)
    {
        _isDirty = true;
        StateHasChanged();

        if (_autoSave)
            await SaveChangesRateLimitedAsync(readDiagram);
    }

    private async Task<Result<WorkflowDefinition, ValidationErrors>> SaveAsync(bool readDiagram, bool publish)
    {
        var workflowDefinition = WorkflowDefinition ?? new WorkflowDefinition();

        if (readDiagram)
        {
            var root = await _diagramDesigner.ReadActivityAsync();
            workflowDefinition.Root = root;
        }

        var saveRequest = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Id = workflowDefinition.Id,
                Description = workflowDefinition.Description,
                Name = workflowDefinition.Name,
                ToolVersion = workflowDefinition.ToolVersion,
                Inputs = workflowDefinition.Inputs,
                Options = workflowDefinition.Options,
                Outcomes = workflowDefinition.Outcomes,
                Outputs = workflowDefinition.Outputs,
                Variables = workflowDefinition.Variables.Select(x => new VariableDefinition
                {
                    Id = x.Id,
                    Name = x.Name,
                    TypeName = x.TypeName,
                    Value = x.Value?.ToString(),
                    IsArray = x.IsArray,
                    StorageDriverTypeName = x.StorageDriverTypeName
                }).ToList(),
                Version = workflowDefinition.Version,
                CreatedAt = workflowDefinition.CreatedAt,
                CustomProperties = workflowDefinition.CustomProperties,
                DefinitionId = workflowDefinition.DefinitionId,
                IsLatest = workflowDefinition.IsLatest,
                IsPublished = workflowDefinition.IsPublished,
                Root = workflowDefinition.Root
            },
            Publish = publish,
        };

        var result = await WorkflowDefinitionService.SaveAsync(saveRequest);
        await result.OnSuccessAsync(async definition => await SetWorkflowDefinitionAsync(definition));

        _isDirty = false;
        StateHasChanged();

        return result;
    }

    private async Task PublishAsync(Func<Task>? onSuccess = default, Action? onFailure = default)
    {
        await SaveChangesAsync(true, true, true, onSuccess, onFailure);
    }

    private bool ShouldUpdateReferences() => WorkflowDefinition!.Options.AutoUpdateConsumingWorkflows;

    private async Task<int> UpdateReferencesAsync()
    {
        var updateReferencesResponse = await WorkflowDefinitionService.UpdateReferencesAsync(WorkflowDefinition!.DefinitionId);
        return updateReferencesResponse.AffectedWorkflows.Count;
    }

    private async Task RetractAsync(Func<Task>? onSuccess = default, Func<ValidationErrors, Task>? onFailure = default)
    {
        var result = await WorkflowDefinitionService.RetractAsync(WorkflowDefinition!.DefinitionId);
        await result.OnSuccessAsync(async definition =>
        {
            await SetWorkflowDefinitionAsync(definition);

            if (onSuccess != null)
                await onSuccess();
        });

        if (onFailure != null)
        {
            await result.OnFailedAsync(async errors => await onFailure(errors));
        }
    }

    private async Task SaveChangesRateLimitedAsync(bool readDiagram)
    {
        await _rateLimitedSaveChangesAsync.InvokeAsync(readDiagram);
    }

    private async Task SaveChangesAsync(bool readDiagram, bool showLoader, bool publish, Func<Task>? onSuccess = default, Action? onFailure = default)
    {
        await InvokeAsync(async () =>
        {
            if (showLoader)
            {
                _isProgressing = true;
                StateHasChanged();
            }

            // Because this method is rate limited, it's possible that the designer has been disposed since the last invocation.
            // Therefore, we need to wrap this in a try/catch block.
            try
            {
                var result = await SaveAsync(readDiagram, publish);
                result.OnSuccess(_ => onSuccess?.Invoke());
                result.OnFailed(errors =>
                {
                    if (onFailure != null)
                    {
                        onFailure();
                        return;
                    }

                    Snackbar.Add(string.Join(Environment.NewLine, errors.Errors.Select(x => x.ErrorMessage)), Severity.Error, options => options.VisibleStateDuration = 5000);
                });
            }
            finally
            {
                if (showLoader)
                {
                    _isProgressing = false;
                    StateHasChanged();
                }
            }
        });
    }

    private async Task ProgressAsync(Func<Task> action)
    {
        _isProgressing = true;
        StateHasChanged();
        await action.Invoke();
        _isProgressing = false;
        StateHasChanged();
    }

    private async Task<T> ProgressAsync<T>(Func<Task<T>> action)
    {
        _isProgressing = true;
        StateHasChanged();
        var result = await action.Invoke();
        _isProgressing = false;
        StateHasChanged();

        return result;
    }

    private void SelectActivity(JsonObject activity)
    {
        // Setting the activity to null first and then requesting an update is a workaround to ensure that BlazorMonaco gets destroyed first.
        // Otherwise, the Monaco editor will not be updated with a new value. Perhaps we should consider updating the Monaco Editor via its imperative API instead of via binding.
        SelectedActivity = null;
        ActivityDescriptor = null;
        StateHasChanged();

        SelectedActivity = activity;
        SelectedActivityId = activity.GetId();
        ActivityDescriptor = ActivityRegistry.Find(activity.GetTypeName(), activity.GetVersion());
        StateHasChanged();
    }

    private async Task SetWorkflowDefinitionAsync(WorkflowDefinition workflowDefinition)
    {
        WorkflowDefinition = workflowDefinition;

        if (WorkflowDefinitionUpdated != null)
            await WorkflowDefinitionUpdated();
    }

    private async Task UpdateActivityPropertiesVisibleHeightAsync()
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _activityPropertiesPaneHeight = (int)visibleHeight - 50;
    }

    private Task OnActivitySelected(JsonObject activity)
    {
        SelectActivity(activity);
        return Task.CompletedTask;
    }

    private async Task OnSelectedActivityUpdated(JsonObject activity)
    {
        _isDirty = true;
        StateHasChanged();
        await _diagramDesigner.UpdateActivityAsync(SelectedActivityId!, activity);
    }

    private async Task OnSaveClick()
    {
        await SaveChangesAsync(true, true, false, () =>
        {
            Snackbar.Add("Workflow saved", Severity.Success);
            return Task.CompletedTask;
        });
    }

    private async Task OnPublishClicked()
    {
        await ProgressAsync(async () => await PublishAsync(async () =>
        {
            // Depending on whether or not the workflow contains Not Found activities, display a different message.
            var graph = await ActivityVisitor.VisitAsync(WorkflowDefinition!);
            var nodes = graph.Flatten();
            var hasNotFoundActivities = nodes.Any(x => x.Activity.GetTypeName() == "Elsa.NotFoundActivity");

            if (hasNotFoundActivities)
                Snackbar.Add("Workflow published with Not Found activities", Severity.Warning, options => options.VisibleStateDuration = 5000);
            else
                Snackbar.Add("Workflow published", Severity.Success);

            if (!ShouldUpdateReferences())
                return;

            var affectedWorkflows = await ProgressAsync(UpdateReferencesAsync);
            Snackbar.Add($"{affectedWorkflows} consuming workflow(s) updated", Severity.Success);
        }));
    }

    private async Task OnRetractClicked()
    {
        await ProgressAsync(async () => await RetractAsync(() =>
        {
            Snackbar.Add("Workflow unpublished", Severity.Success);
            return Task.CompletedTask;
        }, errors =>
        {
            Snackbar.Add(string.Join(Environment.NewLine, errors.Errors, Severity.Error));
            return Task.CompletedTask;
        }));
    }

    private async Task OnWorkflowDefinitionUpdated() => await HandleChangesAsync(false);
    private async Task OnGraphUpdated() => await HandleChangesAsync(true);

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        await UpdateActivityPropertiesVisibleHeightAsync();
    }

    private async Task OnAutoSaveChanged(bool? value)
    {
        _autoSave = value ?? false;

        if (_autoSave)
            await SaveChangesAsync(true, false, false);
    }

    private async Task OnDownloadClicked()
    {
        var download = await WorkflowDefinitionService.ExportDefinitionAsync(WorkflowDefinition!.DefinitionId, VersionOptions.Latest);
        var fileName = $"{WorkflowDefinition.Name.Kebaberize()}.json";
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnUploadClicked()
    {
        await DomAccessor.ClickElementAsync("#workflow-file-upload-button-wrapper input[type=file]");
    }

    private async Task OnFileSelected(IBrowserFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var json = await reader.ReadToEndAsync();

        JsonSerializerOptions serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        serializerOptions.Converters.Add(new VersionOptionsJsonConverter());

        var model = JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, serializerOptions)!;

        // Overwrite the definition ID with the one currently loaded.
        // This will ensure that the imported definition will be saved as a new version of the current definition. 
        model.DefinitionId = WorkflowDefinition!.DefinitionId;

        var workflowDefinition = await WorkflowDefinitionService.ImportDefinitionAsync(model);
        await _diagramDesigner.LoadActivityAsync(workflowDefinition.Root);
        await SetWorkflowDefinitionAsync(workflowDefinition);

        _isDirty = false;

        StateHasChanged();
    }

    private async Task OnRunWorkflowClicked()
    {
        var workflowInstanceId = await ProgressAsync(async () =>
        {
            var request = new ExecuteWorkflowDefinitionRequest
            {
                VersionOptions = VersionOptions.Latest
            };

            var definitionId = WorkflowDefinition!.DefinitionId;
            return await WorkflowDefinitionService.ExecuteAsync(definitionId, request);
        });

        Snackbar.Add("Successfully started workflow", Severity.Success);

        NavigationManager.NavigateTo($"workflows/instances/{workflowInstanceId}/view");
    }
}