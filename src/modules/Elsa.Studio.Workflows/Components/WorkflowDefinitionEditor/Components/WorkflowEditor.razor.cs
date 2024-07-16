using System.IO.Compression;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
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
using Microsoft.Extensions.Logging;
using MudBlazor;
using Radzen;
using Radzen.Blazor;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// <summary>
/// A component that allows the user to edit a workflow definition.
/// </summary>
public partial class WorkflowEditor
{
    private readonly RateLimitedFunc<bool, Task> _rateLimitedSaveChangesAsync;
    private readonly JsonSerializerOptions _jsonSerializerOptions = CreateJsonSerializerOptions();
    private bool _autoSave = true;
    private bool _isDirty;
    private bool _isProgressing;
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private int _activityPropertiesPaneHeight = 300;
    private DiagramDesignerWrapper _diagramDesigner = default!;
    private WorkflowDefinition? _workflowDefinition;

    /// <inheritdoc />
    public WorkflowEditor()
    {
        _rateLimitedSaveChangesAsync = Debouncer.Debounce<bool, Task>(readDiagram => SaveChangesAsync(readDiagram, false, false), TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Gets or sets the drag and drop manager via property injection.
    /// </summary>
    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>
    /// Gets or sets a callback invoked when the workflow definition is updated.
    /// </summary>
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }

    /// <summary>An event that is invoked when a workflow definition has been executed.</summary>
    /// <remarks>The ID of the workflow instance is provided as the value to the event callback.</remarks>
    [Parameter] public EventCallback<string> WorkflowDefinitionExecuted { get; set; }

    /// Gets or sets the event triggered when an activity is selected.
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// Gets the selected activity ID.
    public string? SelectedActivityId { get; private set; }

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;
    [Inject] private ILogger<WorkflowDefinitionEditor> Logger { get; set; } = default!;

    private JsonObject? Activity => _workflowDefinition?.Root;
    private JsonObject? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    private ActivityPropertiesPanel? ActivityPropertiesPanel { get; set; }

    private RadzenSplitterPane ActivityPropertiesPane
    {
        get => _activityPropertiesPane;
        set
        {
            _activityPropertiesPane = value;

            // Prefix the ID with a non-numerical value, so it can always be used as a query selector
            // (sometimes, Radzen generates a unique ID starting with a number).
            _activityPropertiesPane.UniqueID = $"pane-{value.UniqueID}";
        }
    }

    /// Gets or sets a flag indicating whether the workflow definition is dirty.
    public async Task NotifyWorkflowChangedAsync()
    {
        await HandleChangesAsync(false);
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _workflowDefinition = WorkflowDefinition;
        
        await ActivityRegistry.EnsureLoadedAsync();

        if (_workflowDefinition?.Root == null)
            return;

        SelectActivity(_workflowDefinition.Root);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (WorkflowDefinition == _workflowDefinition)
            return;
     
        _workflowDefinition = WorkflowDefinition;
        
        if (_workflowDefinition?.Root == null)
            return;
            
        await _diagramDesigner.LoadActivityAsync(_workflowDefinition!.Root);
        SelectActivity(_workflowDefinition.Root);
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

    private async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(bool readDiagram, bool publish)
    {
        var workflowDefinition = _workflowDefinition ?? new WorkflowDefinition();

        if (readDiagram)
        {
            var root = await _diagramDesigner.GetActivityAsync();
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
                PropertyBag = workflowDefinition.PropertyBag,
                DefinitionId = workflowDefinition.DefinitionId,
                IsLatest = workflowDefinition.IsLatest,
                IsPublished = workflowDefinition.IsPublished,
                Root = workflowDefinition.Root
            },
            Publish = publish,
        };

        var result = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.SaveAsync(saveRequest));
        await result.OnSuccessAsync(async response => await SetWorkflowDefinitionAsync(response.WorkflowDefinition));

        _isDirty = false;
        StateHasChanged();

        return result;
    }

    private async Task PublishAsync(Func<SaveWorkflowDefinitionResponse, Task>? onSuccess = default, Func<ValidationErrors, Task>? onFailure = default)
    {
        await SaveChangesAsync(true, true, true, onSuccess, onFailure);
    }

    private bool ShouldUpdateReferences() => _workflowDefinition!.Options.AutoUpdateConsumingWorkflows;

    private async Task<int> UpdateReferencesAsync()
    {
        var updateReferencesResponse = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.UpdateReferencesAsync(_workflowDefinition!.DefinitionId));
        return updateReferencesResponse.AffectedWorkflows.Count;
    }

    private async Task RetractAsync(Func<Task>? onSuccess = default, Func<ValidationErrors, Task>? onFailure = default)
    {
        var result = await InvokeWithBlazorServiceContext(() => WorkflowDefinitionService.RetractAsync(_workflowDefinition!.DefinitionId));
        await result.OnSuccessAsync(async definition =>
        {
            await SetWorkflowDefinitionAsync(definition);
            if (onSuccess != null) await onSuccess();
        });

        await result.OnFailedAsync(async errors =>
        {
            if (onFailure != null) await onFailure(errors);
        });
    }

    private async Task SaveChangesRateLimitedAsync(bool readDiagram)
    {
        await _rateLimitedSaveChangesAsync.InvokeAsync(readDiagram);
    }

    private async Task SaveChangesAsync(bool readDiagram, bool showLoader, bool publish, Func<SaveWorkflowDefinitionResponse, Task>? onSuccess = default, Func<ValidationErrors, Task>? onFailure = default)
    {
        await InvokeAsync(async () =>
        {
            if (showLoader)
            {
                _isProgressing = true;
                StateHasChanged();
            }

            // Because this method is rate-limited, it's possible that the designer has been disposed of since the last invocation.
            // Therefore, we need to wrap this in a try/catch block.
            try
            {
                var result = await SaveAsync(readDiagram, publish);
                await result.OnSuccessAsync(response =>
                {
                    onSuccess?.Invoke(response);
                    return Task.CompletedTask;
                }).ConfigureAwait(false);

                await result.OnFailedAsync(errors =>
                {
                    onFailure?.Invoke(errors);
                    Snackbar.Add(string.Join(Environment.NewLine, errors.Errors.Select(x => x.ErrorMessage)), Severity.Error, options => options.VisibleStateDuration = 5000);
                    return Task.CompletedTask;
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
        _workflowDefinition = WorkflowDefinition = workflowDefinition;

        if (WorkflowDefinitionUpdated.HasDelegate)
            await WorkflowDefinitionUpdated.InvokeAsync();
    }

    private async Task UpdateActivityPropertiesVisibleHeightAsync()
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _activityPropertiesPaneHeight = (int)visibleHeight - 50;
    }

    private async Task OnActivitySelected(JsonObject activity)
    {
        SelectActivity(activity);
        await ActivitySelected.InvokeAsync(activity);
    }

    private async Task OnSelectedActivityUpdated(JsonObject activity)
    {
        _isDirty = true;
        StateHasChanged();
        await _diagramDesigner.UpdateActivityAsync(SelectedActivityId!, activity);
    }

    private async Task OnSaveClick()
    {
        await SaveChangesAsync(true, true, false, _ =>
        {
            Snackbar.Add("Workflow saved", Severity.Success);
            return Task.CompletedTask;
        });
    }

    private async Task OnPublishClicked()
    {
        await ProgressAsync(async () => await PublishAsync(async response =>
        {
            // Depending on whether the workflow contains Not Found activities, display a different message.
            var graph = await _diagramDesigner.GetActivityGraphAsync();
            var nodes = graph.ActivityNodeLookup.Values;
            var hasNotFoundActivities = nodes.Any(x => x.Activity.GetTypeName() == "Elsa.NotFoundActivity");

            if (hasNotFoundActivities)
                Snackbar.Add("Workflow published with Not Found activities", Severity.Warning, options => options.VisibleStateDuration = 5000);
            else
                Snackbar.Add("Workflow published", Severity.Success);

            if (response.ConsumingWorkflowCount > 0)
            {
                Snackbar.Add($"{response.ConsumingWorkflowCount} consuming workflow(s) updated", Severity.Success, options => options.VisibleStateDuration = 3000);
            }
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
            Snackbar.Add(string.Join(Environment.NewLine, errors, Severity.Error));
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

    private async Task OnExportClicked()
    {
        var download = await WorkflowDefinitionService.ExportDefinitionAsync(_workflowDefinition!.DefinitionId, VersionOptions.Latest);
        var fileName = $"{_workflowDefinition.Name.Kebaberize()}.json";
        if(download.Content.CanSeek) download.Content.Seek(0, SeekOrigin.Begin);
        await Files.DownloadFileFromStreamAsync(fileName, download.Content);
    }

    private async Task OnImportClicked()
    {
        await DomAccessor.ClickElementAsync("#workflow-file-upload-button-wrapper input[type=file]");
    }

    private async Task OnFilesSelected(IReadOnlyList<IBrowserFile>? files)
    {
        if (files == null || files.Count == 0)
            return;
        
        await ImportFilesAsync(files);
        _isDirty = false;

        StateHasChanged();
    }

    private async Task ImportFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        IBrowserFile? importedFile = null;

        _isDirty = true;
        _isProgressing = true;
        StateHasChanged();

        foreach (var file in files)
        {
            var stream = file.OpenReadStream();

            if (file.ContentType == MediaTypeNames.Application.Zip || file.Name.EndsWith(".zip"))
            {
                var success = await ImportZipFileAsync(stream);
                if (success)
                {
                    importedFile = file;
                    break;
                }
            }

            else if (file.ContentType == MediaTypeNames.Application.Json || file.Name.EndsWith(".json"))
            {
                var success = await ImportFromStreamAsync(stream);
                if (success)
                {
                    importedFile = file;
                    break;
                }
            }
        }

        _isProgressing = false;
        _isDirty = false;
        StateHasChanged();

        if (importedFile != null) 
            Snackbar.Add($"Successfully imported workflow definition from file {importedFile.Name}", Severity.Success);
    }

    private async Task<bool> ImportZipFileAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        var zipArchive = new ZipArchive(memoryStream);

        foreach (var entry in zipArchive.Entries)
        {
            if (entry.FullName.EndsWith(".json"))
            {
                await using var entryStream = entry.Open();
                var success = await ImportFromStreamAsync(entryStream);

                if (success)
                    return true;
            }
            else if (entry.FullName.EndsWith(".zip"))
            {
                await using var entryStream = entry.Open();
                var success = await ImportZipFileAsync(entryStream);

                if (success)
                    return true;
            }
        }

        return false;
    }

    private async Task<bool> ImportFromStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        try
        {
            var model = JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, _jsonSerializerOptions)!;

            // Check if this is a workflow definition file.
            if (model.DefinitionId == null!)
                return false;

            // Overwrite the definition ID with the one currently loaded.
            // This will ensure that the imported definition will be saved as a new version of the current definition. 
            model.DefinitionId = _workflowDefinition!.DefinitionId;
            var workflowDefinition = await InvokeWithBlazorServiceContext(async () => await WorkflowDefinitionService.ImportDefinitionAsync(model));
            await _diagramDesigner.LoadActivityAsync(workflowDefinition.Root);
            await SetWorkflowDefinitionAsync(workflowDefinition);
        }
        catch (Exception e)
        {
            Snackbar.Add($"Failed to import workflow definition: {e.Message}", Severity.Error);
        }

        return true;
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new VersionOptionsJsonConverter());
        return options;
    }

    private async Task OnRunWorkflowClicked()
    {
        var workflowInstanceId = await ProgressAsync(async () =>
        {
            var request = new ExecuteWorkflowDefinitionRequest
            {
                VersionOptions = VersionOptions.Latest
            };

            var definitionId = _workflowDefinition!.DefinitionId;
            return await WorkflowDefinitionService.ExecuteAsync(definitionId, request);
        });

        Snackbar.Add("Successfully started workflow", Severity.Success);

        var workflowDefinitionExecuted = this.WorkflowDefinitionExecuted;

        if (workflowDefinitionExecuted.HasDelegate)
            await WorkflowDefinitionExecuted.InvokeAsync(workflowInstanceId);
        else
            NavigationManager.NavigateTo($"workflows/instances/{workflowInstanceId}/view");
    }
}