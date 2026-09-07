using Elsa.Studio.Workflows.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionList;

/// <summary>
/// Validates a <see cref="WorkflowMetadataModel"/> directly against its validator and publishes the result to
/// the edit context's own message store. This keeps the submit decision independent of Blazilla's shared,
/// unversioned field-change validation, which can otherwise overwrite the message store with a stale answer
/// from an earlier asynchronous uniqueness check.
/// </summary>
internal static class WorkflowMetadataValidation
{
    public static async Task<bool> ValidateAndPublishAsync(
        IValidator<WorkflowMetadataModel> validator,
        WorkflowMetadataModel model,
        EditContext editContext,
        ValidationMessageStore validationMessages)
    {
        var result = await validator.ValidateAsync(model);

        validationMessages.Clear();

        foreach (var error in result.Errors)
            validationMessages.Add(new FieldIdentifier(model, error.PropertyName), error.ErrorMessage);

        editContext.NotifyValidationStateChanged();

        return result.IsValid;
    }
}
