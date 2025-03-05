using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Validators;

public class InputModelValidator : AbstractValidator<InputDefinitionModel>
{
    [Inject] private ILocalizer Localizer { get; set; } = default!;

    public InputModelValidator(WorkflowDefinition workflowDefinition)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(Localizer["Please enter a name for the input."]);
        
        RuleFor(x => x.Name)
            .Must((context, name, cancellationToken) =>
            {
                var existingInput = workflowDefinition.Inputs.FirstOrDefault(x => x.Name == name);
                return existingInput == null || existingInput.Name == context.Name;
            })
            .WithMessage(Localizer["An input with this name already exists."]);
    }
}