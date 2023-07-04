using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using Radzen;
using Radzen.Blazor;
using Refit;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit;

public partial class WorkflowEditor
{
    private readonly RateLimitedFunc<bool, Task> _rateLimitedSaveChangesAsync;
    private IDiagramDesigner? _diagramDesigner;
    private bool _autoSave = true;
    private bool _isDirty;
    private bool _isProgressing;
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private int _activityPropertiesPaneHeight = 300;

    public WorkflowEditor()
    {
        _rateLimitedSaveChangesAsync = Debouncer.Debounce<bool, Task>(async readDiagram => await SaveChangesAsync(readDiagram, false, false), TimeSpan.FromMilliseconds(500));
    }

    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IFiles Files { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    private Activity? SelectedActivity { get; set; }
    private ActivityDescriptor? ActivityDescriptor { get; set; }
    public string? SelectedActivityId { get; set; }
    private ActivityProperties.ActivityProperties? ActivityPropertiesTab { get; set; }

    public RadzenSplitterPane ActivityPropertiesPane
    {
        get => _activityPropertiesPane;
        set
        {
            _activityPropertiesPane = value;

            // Prefix the ID with a non-numerical value so it can always be used as a query selector (sometimes, Radzen generates a unique ID starting with a number).
            _activityPropertiesPane.UniqueID = $"pane-{value.UniqueID}";
        }
    }

    private async Task HandleChangesAsync(bool readDiagram)
    {
        _isDirty = true;
        StateHasChanged();

        if (_autoSave)
            await SaveChangesRateLimitedAsync(readDiagram);
    }

    protected override void OnInitialized()
    {
        if (WorkflowDefinition?.Root == null)
            return;

        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(WorkflowDefinition.Root);
    }

    private async Task SaveAsync(bool readDiagram, bool publish)
    {
        var workflowDefinition = WorkflowDefinition ?? new WorkflowDefinition();

        if (readDiagram)
        {
            var root = await _diagramDesigner!.ReadRootActivityAsync();
            workflowDefinition.Root = root;
        }

        var saveRequest = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Id = workflowDefinition.Id,
                Description = workflowDefinition.Description,
                Name = workflowDefinition.Name,
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

        WorkflowDefinition = await WorkflowDefinitionService.SaveAsync(saveRequest);
        _isDirty = false;
        StateHasChanged();
    }

    private async Task PublishAsync()
    {
        await SaveChangesAsync(true, true, true);
    }

    private async Task<ProblemDetails?> RetractAsync()
    {
        try
        {
            WorkflowDefinition = await WorkflowDefinitionService.RetractAsync(WorkflowDefinition!.DefinitionId);
            return null;
        }
        catch (ValidationApiException e)
        {
            return e.Content;
        }
    }

    private async Task SaveChangesRateLimitedAsync(bool readDiagram)
    {
        await _rateLimitedSaveChangesAsync.InvokeAsync(readDiagram);
    }

    private async Task SaveChangesAsync(bool readDiagram, bool showLoader, bool publish)
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
                await SaveAsync(readDiagram, publish);
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

    private async Task OnActivitySelected(Activity activity)
    {
        SelectedActivity = activity;
        SelectedActivityId = activity.Id;
        ActivityDescriptor = await ActivityRegistry.FindAsync(activity.Type);
        StateHasChanged();
    }

    private async Task OnSelectedActivityUpdated(Activity activity)
    {
        _isDirty = true;
        StateHasChanged();
        await _diagramDesigner!.UpdateActivityAsync(SelectedActivityId!, activity);
        SelectedActivityId = activity.Id;
    }

    private async Task OnSaveClick()
    {
        await SaveChangesAsync(true, true, false);
        Snackbar.Add("Workflow saved", Severity.Success);
    }

    private async Task OnPublishClicked()
    {
        await ProgressAsync(PublishAsync);
        Snackbar.Add("Workflow published", Severity.Success);
    }

    private async Task OnRetractClicked()
    {
        var problemDetails = await ProgressAsync(RetractAsync);
        
        if(problemDetails != null)
        {
            var error = problemDetails.Errors.Values.First().First();
            Snackbar.Add(error, Severity.Error);
            return;
        }
        
        Snackbar.Add("Workflow unpublished", Severity.Success);
    }

    private async Task OnWorkflowDefinitionUpdated() => await HandleChangesAsync(false);
    private async Task OnGraphUpdated() => await HandleChangesAsync(true);

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _activityPropertiesPaneHeight = (int)visibleHeight;
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
        serializerOptions.Converters.Add(new ActivityJsonConverterFactory(ServiceProvider));
        serializerOptions.Converters.Add(new ExpressionJsonConverterFactory());

        var model = JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, serializerOptions)!;

        // Overwrite the definition ID with the one currently loaded.
        // This will ensure that the imported definition will be saved as a new version of the current definition. 
        model = model with
        {
            DefinitionId = WorkflowDefinition!.DefinitionId
        };

        WorkflowDefinition = await WorkflowDefinitionService.ImportDefinitionAsync(model);
        await _diagramDesigner!.LoadRootActivity(WorkflowDefinition.Root);
        _isDirty = false;
        StateHasChanged();
    }
}