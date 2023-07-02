namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.ActivityProperties.Tabs.Outputs.Models;

public record BindingTargetGroup(string Text, BindingKind Kind, ICollection<BindingTargetOption> Options);