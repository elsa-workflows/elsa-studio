using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
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
using Variant = MudBlazor.Variant;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;

/// A component that allows the user to edit a workflow definition.
public partial class WorkflowEditor
{
    private readonly RateLimitedFunc<bool, Task> _rateLimitedSaveChangesAsync;
    private bool _autoSave = true;
    private bool _isDirty;
    private RadzenSplitterPane _activityPropertiesPane = null!;
    private int _activityPropertiesPaneHeight = 300;
    private DiagramDesignerWrapper _diagramDesigner = null!;

    /// <inheritdoc />
    public WorkflowEditor()
    {
        _rateLimitedSaveChangesAsync = Debouncer.Debounce<bool, Task>(readDiagram => SaveChangesAsync(readDiagram, false, false), TimeSpan.FromMilliseconds(500));
    }

    /// Gets or sets the drag and drop manager via property injection.
    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = null!;

    /// Gets or sets the workflow definition.
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// Gets or sets a callback invoked when the workflow definition is updated.
    [Parameter] public Func<Task>? WorkflowDefinitionUpdated { get; set; }

    /// Gets or sets the event triggered when an activity is selected.
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    /// Gets the selected activity ID.
    public string? SelectedActivityId { get; private set; }

    [Inject] private IWorkflowDefinitionEditorService WorkflowDefinitionEditorService { get; set; } = null!;
    [Inject] private IWorkflowDefinitionImporter WorkflowDefinitionImporter { get; set; } = null!;
    [Inject] private IActivityVisitor ActivityVisitor { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = null!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = null!;
    [Inject] private IFiles Files { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] private ILogger<WorkflowDefinitionEditor> Logger { get; set; } = null!;
    [Inject] private IWorkflowJsonDetector WorkflowJsonDetector { get; set; } = null!;
    [Inject] private IBackendApiClientProvider BackendApiClientProvider { get; set; } = null!;

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

        var result = await WorkflowDefinitionEditorService.SaveAsync(workflowDefinition, publish, async definition => await SetWorkflowDefinitionAsync(definition));

        _isDirty = false;
        StateHasChanged();

        return result;
    }

    private async Task PublishAsync(Func<SaveWorkflowDefinitionResponse, Task>? onSuccess = null, Func<ValidationErrors, Task>? onFailure = null)
    {
        await SaveChangesAsync(true, true, true, onSuccess, onFailure);
    }

    private async Task RetractAsync(Func<Task>? onSuccess = null, Func<ValidationErrors, Task>? onFailure = null)
    {
        var result = await WorkflowDefinitionEditorService.RetractAsync(_workflowDefinition!, async definition => await SetWorkflowDefinitionAsync(definition));
        await result.OnSuccessAsync(async _ =>
        {
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

    private async Task SaveChangesAsync(bool readDiagram, bool showLoader, bool publish, Func<SaveWorkflowDefinitionResponse, Task>? onSuccess = null, Func<ValidationErrors, Task>? onFailure = null)
    {
        await InvokeAsync(async () =>
        {
            if (showLoader)
            {
                IsProgressing = true;
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
                    foreach (var error in errors.Errors) 
                        Snackbar.Add(error.ErrorMessage, Severity.Error, options => options.VisibleStateDuration = 5000);
                    return Task.CompletedTask;
                });
            }
            finally
            {
                if (showLoader)
                {
                    IsProgressing = false;
                    StateHasChanged();
                }
            }
        });
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
        if (WorkflowDefinitionUpdated != null) await WorkflowDefinitionUpdated();
    }

    private async Task<IWorkflowDefinitionsApi> GetApiAsync(CancellationToken cancellationToken = default)
    {
        return await BackendApiClientProvider.GetApiAsync<IWorkflowDefinitionsApi>(cancellationToken);
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
        if (ActivitySelected != null) await ActivitySelected(activity);
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
            Snackbar.Add(Localizer["Workflow saved"], Severity.Success);
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
                Snackbar.Add(Localizer["Workflow published with Not Found activities"], Severity.Warning, options => options.VisibleStateDuration = 5000);
            else
                Snackbar.Add(Localizer["Workflow published"], Severity.Success);

            if (response.ConsumingWorkflowCount > 0)
            {
                Snackbar.Add(Localizer["{0} consuming workflow(s) updated", response.ConsumingWorkflowCount], Severity.Success, options => options.VisibleStateDuration = 3000);
            }
        }));
    }

    private async Task OnRetractClicked()
    {
        await ProgressAsync(async () => await RetractAsync(() =>
        {
            Snackbar.Add(Localizer["Workflow unpublished"], Severity.Success);
            return Task.CompletedTask;
        }, errors =>
        {
            Snackbar.Add(string.Join(Environment.NewLine, errors), Severity.Error);
            return Task.CompletedTask;
        }));
    }

    //private async Task OnWorkflowDefinitionUpdated() => await HandleChangesAsync(false);
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
        var download = await WorkflowDefinitionEditorService.ExportAsync(_workflowDefinition!);
        var fileName = $"{_workflowDefinition!.Name.Kebaberize()}.json";
        if (download.Content.CanSeek) download.Content.Seek(0, SeekOrigin.Begin);
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
        _isDirty = true;
        IsProgressing = true;
        StateHasChanged();

        var options = new ImportOptions
        {
            DefinitionId = WorkflowDefinition?.DefinitionId,
            ImportedCallback = async definition =>
            {
                await SetWorkflowDefinitionAsync(definition);
                await _diagramDesigner.LoadActivityAsync(definition.Root);
            },
            ErrorCallback = ex =>
            {
                Snackbar.Add($"Failed to import workflow definition: {ex.Message}", Severity.Error);
                return Task.CompletedTask;
            }
        };
        var importResults = (await WorkflowDefinitionImporter.ImportFilesAsync(files, options)).ToList();
        var failedImports = importResults.Where(x => !x.IsSuccess).ToList();
        var successfulImports = importResults.Where(x => x.IsSuccess).ToList();

        IsProgressing = false;
        _isDirty = false;
        StateHasChanged();

        if (importResults.Count == 0)
        {
            Snackbar.Add(Localizer["No workflows were imported."], Severity.Info);
            return;
        }

        if (successfulImports.Count == 1)
            Snackbar.Add(Localizer["Successfully imported 1 workflow definition."], Severity.Success, ConfigureSnackbar);
        else if (importResults.Count > 1)
            Snackbar.Add(Localizer["Successfully imported {0} workflow definitions.", importResults.Count], Severity.Success, ConfigureSnackbar);

        if (failedImports.Count == 1)
            Snackbar.Add(Localizer["Failed to import 1 workflow definition: {0}", failedImports[0].Failure!.ErrorMessage], Severity.Error, ConfigureSnackbar);
        else if (failedImports.Count > 1) 
            Snackbar.Add(Localizer["Failed to import {0} workflow definitions. Errors: {1}", failedImports.Count, string.Join(", ", failedImports.Select(x => x.Failure!.ErrorMessage))], Severity.Error, ConfigureSnackbar);

        return;
        void ConfigureSnackbar(SnackbarOptions snackbarOptions)
        {
            snackbarOptions.SnackbarVariant = Variant.Filled;
            snackbarOptions.CloseAfterNavigation = failedImports.Count > 0;
            snackbarOptions.VisibleStateDuration = failedImports.Count > 0 ? 10000 : 3000;
        }
    }
}