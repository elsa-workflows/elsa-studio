using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Models;
using FluentValidation;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Validators;

public class OutputModelValidator : AbstractValidator<OutputDefinitionModel>
{
    public OutputModelValidator(WorkflowDefinition workflowDefinition)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the output.");
        
        RuleFor(x => x.Name)
            .Must((context, name, cancellationToken) =>
            {
                var existingOutput = workflowDefinition.Outputs.FirstOrDefault(x => x.Name == name);
                return existingOutput == null || existingOutput.Name == context.Name;
            })
            .WithMessage("An input with this name already exists.");
    }
}