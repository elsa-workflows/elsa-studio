using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Workflows.Validators;

/// <summary>
/// A validator for <see cref="WorkflowMetadataModel"/> instances.
/// </summary>
public class WorkflowPropertiesModelValidator : AbstractValidator<WorkflowMetadataModel>
{
    /// <inheritdoc />
    [Inject] private ILocalizer _localizer { get; set; } = default!;
    public WorkflowPropertiesModelValidator(IWorkflowDefinitionService workflowDefinitionService)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(_localizer["Please enter a name for the workflow"]);
        
        RuleFor(x => x.Name)
            .MustAsync((context, name, cancellationToken) => workflowDefinitionService.GetIsNameUniqueAsync(name!, context.DefinitionId, cancellationToken))
            .WithMessage(_localizer["A workflow with this name already exists"]);
    }
}