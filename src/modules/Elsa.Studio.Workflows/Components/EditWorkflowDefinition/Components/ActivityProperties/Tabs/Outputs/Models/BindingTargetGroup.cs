namespace Elsa.Studio.Workflows.Components.EditWorkflowDefinition.Components.ActivityProperties.Tabs.Outputs.Models;

public record BindingTargetGroup(string Text, BindingKind Kind, ICollection<BindingTargetOption> Options);