using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Components.EditWorkflowDefinition.Components.WorkflowProperties.Tabs.Variables.Models;
using FluentValidation;

namespace Elsa.Studio.Workflows.Components.EditWorkflowDefinition.Components.WorkflowProperties.Tabs.Variables.Validators;

public class VariableModelValidator : AbstractValidator<VariableModel>
{
    public VariableModelValidator(WorkflowDefinition workflowDefinition)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the variable.");
        
        RuleFor(x => x.Name)
            .Must((context, name, cancellationToken) =>
            {
                var existingVariable = workflowDefinition.Variables.FirstOrDefault(x => x.Name == name);
                return existingVariable == null || existingVariable.Id == context.Name;
            })
            .WithMessage("A variable with this name already exists in the current scope.");
    }
}