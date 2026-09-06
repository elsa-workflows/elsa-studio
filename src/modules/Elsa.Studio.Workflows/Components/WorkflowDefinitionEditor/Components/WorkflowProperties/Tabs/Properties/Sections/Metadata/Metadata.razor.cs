using Blazored.FluentValidation;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Properties.Sections.Metadata;

/// <summary>
/// A component that renders the workflow definition metadata.
/// </summary>
public partial class Metadata
{
    private readonly WorkflowMetadataModel _model = new();
    private FluentValidationValidator _fluentValidationValidator = default!;
    private WorkflowPropertiesModelValidator _validator = default!;
    private EditContext _editContext = default!;
    private WorkflowDefinition? _boundWorkflowDefinition;
    private string? _lastLoadedName;
    private string? _lastLoadedDescription;
    private string? _lastCommittedName;
    private string? _lastCommittedDescription;
    private long _editVersion;
    private long _workflowVersion;
    private bool _hasCommittedMetadata;

    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    [Parameter] public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the callback that is invoked when the workflow definition is updated.
    /// </summary>
    [Parameter] public EventCallback WorkflowDefinitionUpdated { get; set; }
    [CascadingParameter] private IWorkspace? Workspace { get; set; }
    
    private bool IsReadOnly => Workspace?.IsReadOnly ?? false;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_boundWorkflowDefinition, WorkflowDefinition))
            return;

        var isSameWorkflowVersion = IsSameWorkflowVersion(_boundWorkflowDefinition, WorkflowDefinition);

        // A save response creates a new object for the same workflow version. Keep local edits and
        // committed metadata across same-version responses, and use a different version as the
        // explicit reload boundary.
        if (isSameWorkflowVersion && (HasLocalChanges || _hasCommittedMetadata))
        {
            // The editor can receive an older save response after this form has already committed a
            // newer local value. Keep the parent object aligned with the last validated submission;
            // later edits may still be unvalidated.
            if (_hasCommittedMetadata && !HasMatchingCommittedMetadata(WorkflowDefinition))
            {
                WorkflowDefinition.Name = _lastCommittedName;
                WorkflowDefinition.Description = _lastCommittedDescription;
            }

            _boundWorkflowDefinition = WorkflowDefinition;
            _lastLoadedName = WorkflowDefinition.Name;
            _lastLoadedDescription = WorkflowDefinition.Description;

            return;
        }

        _boundWorkflowDefinition = WorkflowDefinition;
        _model.DefinitionId = WorkflowDefinition.DefinitionId;
        _model.Description = WorkflowDefinition.Description;
        _model.Name = WorkflowDefinition.Name;
        _editContext = new EditContext(_model);
        _lastLoadedName = WorkflowDefinition.Name;
        _lastLoadedDescription = WorkflowDefinition.Description;
        _editVersion++;
        _workflowVersion++;
        _hasCommittedMetadata = false;
        _lastCommittedName = null;
        _lastCommittedDescription = null;
    }

    private Task ValidateForm() => ValidateAndCommitAsync();

    private Task OnSubmit(EditContext _) => ValidateAndCommitAsync();

    private async Task ValidateAndCommitAsync()
    {
        var workflowVersion = _workflowVersion;
        var editVersion = _editVersion;
        var workflowDefinition = WorkflowDefinition;
        var validator = _fluentValidationValidator;

        if (!await validator.ValidateAsync())
            return;

        if (workflowVersion != _workflowVersion || editVersion != _editVersion ||
            !IsSameWorkflowVersion(workflowDefinition, WorkflowDefinition))
            return;

        await CommitAsync(workflowVersion, editVersion, workflowDefinition);
    }

    private Task OnNameChanged(string value)
    {
        if (string.Equals(_model.Name, value, StringComparison.Ordinal))
            return Task.CompletedTask;

        _model.Name = value;
        _editVersion++;
        return Task.CompletedTask;
    }

    private Task OnDescriptionChanged(string value)
    {
        if (string.Equals(_model.Description, value, StringComparison.Ordinal))
            return Task.CompletedTask;

        _model.Description = value;
        _editVersion++;
        return Task.CompletedTask;
    }

    private async Task CommitAsync(long workflowVersion, long editVersion, WorkflowDefinition workflowDefinition)
    {
        if (workflowVersion != _workflowVersion || editVersion != _editVersion ||
            !IsSameWorkflowVersion(workflowDefinition, WorkflowDefinition))
            return;

        WorkflowDefinition.Description = _model.Description;
        WorkflowDefinition.Name = _model.Name;
        _lastLoadedName = _model.Name;
        _lastLoadedDescription = _model.Description;
        _lastCommittedName = _model.Name;
        _lastCommittedDescription = _model.Description;
        _hasCommittedMetadata = true;

        if (WorkflowDefinitionUpdated.HasDelegate)
            await WorkflowDefinitionUpdated.InvokeAsync();
    }

    private bool HasLocalChanges => !string.Equals(_model.Name, _lastLoadedName, StringComparison.Ordinal) ||
                                    !string.Equals(_model.Description, _lastLoadedDescription, StringComparison.Ordinal);

    private bool HasMatchingCommittedMetadata(WorkflowDefinition definition) =>
        _hasCommittedMetadata &&
        string.Equals(_lastCommittedName, definition.Name, StringComparison.Ordinal) &&
        string.Equals(_lastCommittedDescription, definition.Description, StringComparison.Ordinal);

    private static bool IsSameWorkflowVersion(WorkflowDefinition? first, WorkflowDefinition? second) =>
        first != null && second != null &&
        string.Equals(first.DefinitionId, second.DefinitionId, StringComparison.Ordinal) &&
        string.Equals(first.Id, second.Id, StringComparison.Ordinal);
}
