using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Models;
using FluentValidation;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Validators;

public class InputModelValidator : AbstractValidator<InputDefinitionModel>
{
    public InputModelValidator(WorkflowDefinition workflowDefinition)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the input.");
        
        RuleFor(x => x.Name)
            .Must((context, name, cancellationToken) =>
            {
                var existingInput = workflowDefinition.Inputs.FirstOrDefault(x => x.Name == name);
                return existingInput == null || existingInput.Name == context.Name;
            })
            .WithMessage("An input with this name already exists.");
    }
}