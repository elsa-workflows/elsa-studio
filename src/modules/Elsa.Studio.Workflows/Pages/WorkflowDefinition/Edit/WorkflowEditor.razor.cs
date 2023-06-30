using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Radzen;
using Radzen.Blazor;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit;

using WorkflowDefinition = Api.Client.Resources.WorkflowDefinitions.Models.WorkflowDefinition;

public partial class WorkflowEditor
{
    private readonly RateLimitedFunc<Task> _rateLimitedSaveChangesAsync;
    private IDiagramDesigner? _diagramDesigner;
    private bool _autoSave = true;
    private bool _isDirty;
    private bool _isSaving;
    private RadzenSplitterPane _activityPropertiesPane = default!;
    private int _activityPropertiesPaneHeight = 300;

    public WorkflowEditor()
    {
        _rateLimitedSaveChangesAsync = Debouncer.Debounce(() => SaveChangesAsync(false), TimeSpan.FromMilliseconds(500));
    }

    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDomAccessor DomAccessor { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private Activity? SelectedActivity { get; set; }
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

    protected override void OnInitialized()
    {
        if (WorkflowDefinition?.Root == null)
            return;

        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(WorkflowDefinition.Root);
    }

    private async Task SaveAsync(Activity root)
    {
        var workflowDefinition = WorkflowDefinition ?? new WorkflowDefinition();

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
                Root = root
            },
            Publish = false,
        };

        WorkflowDefinition = await WorkflowDefinitionService.SaveAsync(saveRequest);
        _isDirty = false;
        StateHasChanged();
    }

    private Task OnActivitySelected(Activity activity)
    {
        SelectedActivity = activity;
        SelectedActivityId = activity.Id;
        StateHasChanged();
        return Task.CompletedTask;
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
        await SaveChangesAsync();
        Snackbar.Add("Workflow saved", Severity.Success);
    }

    private async Task OnGraphUpdated()
    {
        _isDirty = true;
        StateHasChanged();
        
        if (_autoSave)
            await SaveChangesRateLimitedAsync();
    }

    private async Task SaveChangesRateLimitedAsync()
    {
        await _rateLimitedSaveChangesAsync.InvokeAsync();
    }

    private async Task SaveChangesAsync(bool showLoader = true)
    {
        await InvokeAsync(async () =>
        {
            if (showLoader)
            {
                _isSaving = true;
                StateHasChanged();
            }

            // Because this method is rate limited, it's possible that the designer has been disposed since the last invocation.
            // Therefore, we need to wrap this in a try/catch block.
            try
            {
                var root = await _diagramDesigner!.ReadRootActivityAsync();
                await SaveAsync(root);
            }
            finally
            {
                if (showLoader)
                {
                    _isSaving = false;
                    StateHasChanged();
                }
            }
        });
    }

    private async Task OnResize(RadzenSplitterResizeEventArgs arg)
    {
        var paneQuerySelector = $"#{ActivityPropertiesPane.UniqueID}";
        var visibleHeight = await DomAccessor.GetVisibleHeightAsync(paneQuerySelector);
        _activityPropertiesPaneHeight = (int)visibleHeight;
    }
}