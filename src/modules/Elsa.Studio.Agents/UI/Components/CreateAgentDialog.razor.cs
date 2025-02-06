using Blazored.FluentValidation;
using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Validators;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Components;

/// A dialog that allows the user to create a new agent.
public partial class CreateAgentDialog
{
    private readonly AgentInputModel _agentInputModel = new();
    private EditContext _editContext = null!;
    private FluentValidationValidator _fluentValidationValidator = null!;
    private AgentInputModelValidator _validator = null!;
    
    /// The default name of the agent to create.
    [Parameter] public string AgentName { get; set; } = "";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    private ICollection<ServiceModel> AvailableServices { get; set; } = [];
    private IReadOnlyCollection<string> SelectedServices { get; set; } = [];
    private ICollection<PluginDescriptorModel> AvailablePlugins { get; set; } = [];
    private IReadOnlyCollection<string> SelectedPlugins { get; set; } = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _agentInputModel.Name = AgentName;
        _agentInputModel.PromptTemplate = "You are a helpful assistant.";
        _agentInputModel.Description = "A helpful assistant.";
        _agentInputModel.FunctionName = "Reply";
        _agentInputModel.OutputVariable.Type = "object";
        _agentInputModel.OutputVariable.Description = "The output of the agent.";
        _agentInputModel.ExecutionSettings.ResponseFormat = "json_object";
        _editContext = new(_agentInputModel);
        var agentsApi = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        var servicesApi = await ApiClientProvider.GetApiAsync<IServicesApi>();
        var pluginsApi = await ApiClientProvider.GetApiAsync<IPluginsApi>();
        _validator = new(agentsApi);
        var servicesResponseList = await servicesApi.ListAsync();
        var pluginsResponseList = await pluginsApi.ListAsync();
        AvailableServices = servicesResponseList.Items;
        AvailablePlugins = pluginsResponseList.Items;
        SelectedServices = _agentInputModel.Services.ToList().AsReadOnly();
        SelectedPlugins = _agentInputModel.Plugins.ToList().AsReadOnly();
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        if(!await _fluentValidationValidator.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private Task OnValidSubmit()
    {
        _agentInputModel.Services = SelectedServices.ToList();
        _agentInputModel.Plugins = SelectedPlugins.ToList();
        MudDialog.Close(_agentInputModel);
        ActivityRegistry.MarkStale();
        ActivityDisplaySettingsRegistry.MarkStale();
        return Task.CompletedTask;
    }
}