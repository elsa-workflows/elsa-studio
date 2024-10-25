using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using FluentValidation;

namespace Elsa.Studio.Workflows.Validators;

/// <summary>
/// A validator for <see cref="WorkflowMetadataModel"/> instances.
/// </summary>
public class WorkflowPropertiesModelValidator : AbstractValidator<WorkflowMetadataModel>
{
    /// <inheritdoc />
    public WorkflowPropertiesModelValidator(IWorkflowDefinitionService workflowDefinitionService)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Please enter a name for the workflow.");
        
        RuleFor(x => x.Name)
            .MustAsync((context, name, cancellationToken) => workflowDefinitionService.GetIsNameUniqueAsync(name!, context.DefinitionId, cancellationToken))
            .WithMessage("A workflow with this name already exists.");
    }
}