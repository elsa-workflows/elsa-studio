using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Components.Outputs;

public partial class OutputsSection
{
    private ICollection<StorageDriverDescriptor> _storageDriverDescriptors = new List<StorageDriverDescriptor>();

    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    [Parameter] public Func<Task>? OnWorkflowDefinitionUpdated { get; set; }
    [Inject] IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] IDialogService DialogService { get; set; } = default!;

    private ICollection<OutputDefinition> Outputs => WorkflowDefinition.Outputs;

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

    private async Task OpenOutputEditorDialog(OutputDefinition? outputDefinition)
    {
        var isNew = outputDefinition == null;

        var parameters = new DialogParameters<EditOutputDialog>
        {
            [nameof(EditOutputDialog.WorkflowDefinition)] = WorkflowDefinition
        };

        if (!isNew)
            parameters[nameof(EditOutputDialog.Output)] = outputDefinition;

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var title = outputDefinition == null ? "Create output" : "Edit output";
        var dialog = await DialogService.ShowAsync<EditOutputDialog>(title, parameters, options);
        var result = await dialog.Result;

        if (result.Canceled)
            return;

        if (isNew)
        {
            outputDefinition = (OutputDefinition)result.Data;
            WorkflowDefinition.Outputs.Add(outputDefinition);
        }

        await RaiseWorkflowDefinitionUpdatedAsync();
    }

    private async Task OnEditClicked(OutputDefinition input)
    {
        await OpenOutputEditorDialog(input);
    }

    private async Task OnDeleteClicked(OutputDefinition input)
    {
        var result = await DialogService.ShowMessageBox("Delete selected output?", "Are you sure you want to delete the selected output?", yesText: "Delete", cancelText: "Cancel");

        if (result != true)
            return;

        WorkflowDefinition.Outputs.Remove(input);

        await RaiseWorkflowDefinitionUpdatedAsync();
    }

    private async Task OnAddOutputClicked()
    {
        await OpenOutputEditorDialog(null);
    }
}