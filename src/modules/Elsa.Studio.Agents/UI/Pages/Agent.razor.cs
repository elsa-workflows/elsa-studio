using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Validators;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Pages;

public partial class Agent : StudioComponentBase
{
    /// The ID of the agent to edit.
    [Parameter] public string AgentId { get; set; } = default!;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private bool UseJsonResponse
    {
        get => _agent.ExecutionSettings.ResponseFormat == "json_object";
        set => _agent.ExecutionSettings.ResponseFormat = value ? "json_object" : "string";
    }

    private ICollection<ServiceModel> AvailableServices { get; set; } = [];
    private IReadOnlyCollection<string> SelectedServices { get; set; } = [];
    
    private ICollection<PluginDescriptorModel> AvailablePlugins { get; set; } = [];
    private IReadOnlyCollection<string> SelectedPlugins { get; set; } = [];


    private MudForm _form = default!;
    private AgentInputModelValidator _validator = default!;
    private AgentModel _agent = new();
    private InputVariableConfig? _inputVariableBackup;
    private MudTable<InputVariableConfig> _inputVariableTable;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        _validator = new AgentInputModelValidator(apiClient);
        var servicesApi = await ApiClientProvider.GetApiAsync<IServicesApi>();
        var pluginsApi = await ApiClientProvider.GetApiAsync<IPluginsApi>();
        var servicesResponseList = await servicesApi.ListAsync();
        var pluginsResponseList = await pluginsApi.ListAsync();
        AvailableServices = servicesResponseList.Items;
        AvailablePlugins = pluginsResponseList.Items;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        _agent = await apiClient.GetAsync(AgentId);
        SelectedServices = _agent.Services.ToList().AsReadOnly();
        SelectedPlugins = _agent.Plugins.ToList().AsReadOnly();
    }

    private async Task OnSaveClicked()
    {
        await _form.Validate();

        if (!_form.IsValid)
            return;

        _agent.Services = SelectedServices.ToList();
        _agent.Plugins = SelectedPlugins.ToList();
        var apiClient = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        _agent = await apiClient.UpdateAsync(AgentId, _agent);
        Snackbar.Add("Agent successfully updated.", Severity.Success);
        StateHasChanged();
    }

    private void OnAddInputVariableClicked()
    {
        var newInputVariable = new InputVariableConfig
        {
            Name = "Variable1",
            Type = "string"
        };

        _agent.InputVariables.Add(newInputVariable);

        // Need to do it this way, otherwise MudTable doesn't show the item in edit mode.
        _ = Task.Delay(1).ContinueWith(_ =>
        {
            InvokeAsync(() =>
            {
                _inputVariableTable.SetEditingItem(newInputVariable);
                StateHasChanged();
            });
        });
    }

    private void BackupInputVariable(object obj)
    {
        var inputVariable = (InputVariableConfig)obj;
        _inputVariableBackup = new InputVariableConfig
        {
            Name = inputVariable.Name,
            Type = inputVariable.Type,
            Description = inputVariable.Description
        };
    }

    private void RestoreInputVariable(object obj)
    {
        var inputVariable = (InputVariableConfig)obj;
        inputVariable.Name = _inputVariableBackup!.Name;
        inputVariable.Type = _inputVariableBackup.Type;
        inputVariable.Description = _inputVariableBackup.Description;
        _inputVariableBackup = null;
    }

    private void CommitInputVariable(object obj)
    {
        _inputVariableBackup = null;
    }

    private Task DeleteInputVariable(InputVariableConfig item)
    {
        _agent.InputVariables.Remove(item);
        StateHasChanged();
        return Task.CompletedTask;
    }
}