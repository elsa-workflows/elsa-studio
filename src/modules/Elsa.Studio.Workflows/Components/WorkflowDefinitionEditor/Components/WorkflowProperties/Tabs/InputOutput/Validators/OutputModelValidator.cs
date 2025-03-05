using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Validators;

public class OutputModelValidator : AbstractValidator<OutputDefinitionModel>
{
    [Inject] private ILocalizer Localizer { get; set; } = default!;
    public OutputModelValidator(WorkflowDefinition workflowDefinition)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(Localizer["Please enter a name for the output."]);
        
        RuleFor(x => x.Name)
            .Must((context, name, cancellationToken) =>
            {
                var existingOutput = workflowDefinition.Outputs.FirstOrDefault(x => x.Name == name);
                return existingOutput == null || existingOutput.Name == context.Name;
            })
            .WithMessage(Localizer["An input with this name already exists."]);
    }
}