using Blazored.FluentValidation;
using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Validators;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Components;

/// A dialog that allows the user to create a new agent.
public partial class CreateAgentDialog
{
    private readonly AgentInputModel _agentInputModel = new();
    private EditContext _editContext = default!;
    private FluentValidationValidator _fluentValidationValidator = default!;
    private AgentInputModelValidator _validator = default!;
    
    /// The default name of the agent to create.
    [Parameter] public string AgentName { get; set; } = "New workflow";
    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    private ICollection<ServiceModel> AvailableServices { get; set; } = [];
    private IReadOnlyCollection<string> SelectedServices { get; set; } = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var servicesApi = await ApiClientProvider.GetApiAsync<IServicesApi>();
        var response = await servicesApi.ListAsync();
        AvailableServices = response.Items;
    }

    protected override async Task OnParametersSetAsync()
    {
        _agentInputModel.Name = AgentName;
        _agentInputModel.OutputVariable.Type = "object";
        _agentInputModel.OutputVariable.Description = "The output of the agent.";
        _agentInputModel.ExecutionSettings.ResponseFormat = "json_object";
        _editContext = new EditContext(_agentInputModel);
        var agentsApi = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        _validator = new AgentInputModelValidator(agentsApi, BlazorServiceAccessor, Services);
        SelectedServices = _agentInputModel.Services.ToList().AsReadOnly();
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
        MudDialog.Close(_agentInputModel);
        return Task.CompletedTask;
    }
}