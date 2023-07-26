namespace Elsa.Studio.Workflows.Screens.EditWorkflowDefinition.Components.ActivityProperties.Tabs.Outputs.Models;

public record BindingTargetGroup(string Text, BindingKind Kind, ICollection<BindingTargetOption> Options);