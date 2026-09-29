using Elsa.Studio.Authorization;

namespace Elsa.Studio.Workflows.Extensions;

/// Workflow-specific permission checks.
public static class UserPermissionsExtensions
{
    /// <summary>
    /// Whether the user can open a workflow instance in the alterations editor, whose page requires alterations:execute
    /// as well as viewing the instance and its definition.
    /// </summary>
    public static bool CanAlterInstances(this UserPermissions permissions) =>
        permissions.Has(WorkflowPermissions.Alterations, PermissionVerbs.Execute)
        && permissions.Has(WorkflowPermissions.Instances, PermissionVerbs.View)
        && permissions.Has(WorkflowPermissions.Definitions, PermissionVerbs.View);
}
