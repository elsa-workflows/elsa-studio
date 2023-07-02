using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using FluentValidation;

namespace Elsa.Studio.Workflows.Validators;

public class WorkflowPropertiesModelValidator : AbstractValidator<WorkflowMetadataModel>
{
    public WorkflowPropertiesModelValidator(IWorkflowDefinitionService workflowDefinitionService)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the workflow.");
        
        RuleFor(x => x.Name)
            .MustAsync(async (context, name, cancellationToken) => await workflowDefinitionService.GetIsNameUniqueAsync(name!, context.DefinitionId, cancellationToken))
            .WithMessage("A workflow with this name already exists.");
    }
}