using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Components.Inputs;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Components.Outputs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.Variables.Components;

public partial class VariablesTab
{
    private ICollection<StorageDriverDescriptor> _storageDriverDescriptors = new List<StorageDriverDescriptor>();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }
    [CascadingParameter] public IWorkspace? Workspace { get; set; }
    [Inject] IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;

    private bool IsReadOnly => Workspace?.IsReadOnly ?? true;
    private ICollection<Variable> Variables => WorkflowDefinition.Variables;

    protected override async Task OnInitializedAsync()
    {
        _storageDriverDescriptors = (await StorageDriverService.GetStorageDriversAsync()).ToList();
    }

    private StorageDriverDescriptor? GetStorageDriverDescriptor(string typeName)
    {
        return _storageDriverDescriptors.FirstOrDefault(x => x.TypeName == typeName);
    }

    private async Task RaiseWorkflowDefinitionUpdatedAsync()
    {
        if (OnWorkflowDefinitionUpdated != null)
            await OnWorkflowDefinitionUpdated();
    }

    private async Task OpenVariableEditorDialog(Variable? variable)
    {
        var isNew = variable == null;

        var parameters = new DialogParameters<EditOutputDialog>
        {
            [nameof(EditVariableDialog.WorkflowDefinition)] = WorkflowDefinition
        };

        if (!isNew)
            parameters[nameof(EditVariableDialog.Variable)] = variable;

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var title = variable == null ? "Create variable" : "Edit variable";
        var dialog = await DialogService.ShowAsync<EditVariableDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (result.Canceled)
            return;

        if (isNew)
        {
            variable = (Variable)result.Data;
            WorkflowDefinition.Variables.Add(variable);
        }

        await RaiseWorkflowDefinitionUpdatedAsync();
    }

    private async Task OnEditClicked(Variable variable)
    {
        await OpenVariableEditorDialog(variable);
    }

    private async Task OnDeleteClicked(Variable variable)
    {
        var result = await DialogService.ShowMessageBox("Delete selected variable?", "Are you sure you want to delete the selected variable?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        WorkflowDefinition.Variables.Remove(variable);

        await RaiseWorkflowDefinitionUpdatedAsync();
    }

    private async Task OnAddVariableClicked()
    {
        await OpenVariableEditorDialog(null);
    }
}