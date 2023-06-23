using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ThrottleDebounce;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit;

using WorkflowDefinition = Api.Client.Resources.WorkflowDefinitions.Models.WorkflowDefinition;

public partial class WorkflowEditor
{
    private readonly RateLimitedFunc<Task> _rateLimitedSaveChangesAsync;
    private IDiagramEditor? _diagramEditor;
    private bool _autoSave = true;
    private bool _isSaving;

    public WorkflowEditor()
    {
        _rateLimitedSaveChangesAsync = Debouncer.Debounce(SaveChangesAsync, TimeSpan.FromMilliseconds(500));
    }

    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;
    [Inject] private IDiagramEditorService DiagramEditorService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private Activity? SelectedActivity { get; set; }
    private ActivityPropertiesTabs? ActivityPropertiesTab { get; set; }

    protected override void OnInitialized()
    {
        if(WorkflowDefinition?.Root == null)
            return;

        _diagramEditor = DiagramEditorService.GetDiagramEditor(WorkflowDefinition.Root);
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
    }

    private Task OnActivitySelected(Activity activity)
    {
        SelectedActivity = activity;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task OnSelectedActivityUpdated(Activity activity)
    {
        await _diagramEditor!.UpdateActivityAsync(activity);
    }

    private async Task OnSaveClick()
    {
        await SaveChangesAsync();
        Snackbar.Add("Workflow saved", Severity.Success);
    }

    private async Task OnGraphUpdated()
    {
        if (_autoSave)
            await SaveChangesRateLimitedAsync();
    }

    private async Task SaveChangesRateLimitedAsync() => await _rateLimitedSaveChangesAsync.InvokeAsync();

    private async Task SaveChangesAsync()
    {
        await InvokeAsync(async () =>
        {
            _isSaving = true;
            StateHasChanged();

            var root = await _diagramEditor!.ReadRootActivityAsync();
            await SaveAsync(root);

            _isSaving = false;
            StateHasChanged();
        });
    }
}