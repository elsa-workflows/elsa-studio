using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

/// <summary>
/// Validates StateMachine designer graphs.
/// </summary>
public class StateMachineValidator
{
    private const string InvalidJsonSlotProperty = "$invalidJson";

    /// <summary>
    /// Validates the specified graph.
    /// </summary>
    public IReadOnlyCollection<StateMachineValidationIssue> Validate(StateMachineGraph graph)
    {
        var issues = new List<StateMachineValidationIssue>();
        var names = graph.States.Select(x => x.Name).ToList();
        var nameCounts = names.GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

        foreach (var state in graph.States)
        {
            if (string.IsNullOrWhiteSpace(state.Name))
                issues.Add(Error("EmptyStateName", "State name is required.", "state"));
            else if (nameCounts[state.Name] > 1)
                issues.Add(Error("DuplicateStateName", $"State name '{state.Name}' is used more than once.", state.Name));

            AddInvalidJsonSlotIssue(issues, state.Entry, $"{state.Name}.entry");
            AddInvalidJsonSlotIssue(issues, state.Exit, $"{state.Name}.exit");
        }

        foreach (var transition in graph.Transitions)
        {
            var transitionTarget = GetTransitionTarget(transition);

            if (string.IsNullOrWhiteSpace(transition.From))
                issues.Add(Error("MissingTransitionSource", "Transition source state is required.", transitionTarget));
            else if (!nameCounts.TryGetValue(transition.From, out var sourceCount) || sourceCount == 0)
                issues.Add(Error("MissingTransitionSourceState", $"Transition source state '{transition.From}' does not exist.", transitionTarget));
            else if (sourceCount > 1)
                issues.Add(Error("AmbiguousTransitionSourceState", $"Transition source state '{transition.From}' matches multiple states.", transitionTarget));

            if (string.IsNullOrWhiteSpace(transition.To))
                issues.Add(Error("MissingTransitionTarget", "Transition target state is required.", transitionTarget));
            else if (!nameCounts.TryGetValue(transition.To, out var targetCount) || targetCount == 0)
                issues.Add(Error("MissingTransitionTargetState", $"Transition target state '{transition.To}' does not exist.", transitionTarget));
            else if (targetCount > 1)
                issues.Add(Error("AmbiguousTransitionTargetState", $"Transition target state '{transition.To}' matches multiple states.", transitionTarget));

            AddInvalidJsonSlotIssue(issues, transition.Trigger, $"{transitionTarget}.trigger");
            AddInvalidJsonSlotIssue(issues, transition.Condition, $"{transitionTarget}.condition");
            AddInvalidJsonSlotIssue(issues, transition.Action, $"{transitionTarget}.action");
        }

        if (!string.IsNullOrWhiteSpace(graph.InitialState) && !nameCounts.ContainsKey(graph.InitialState))
            issues.Add(Warning("MissingInitialState", $"Initial state '{graph.InitialState}' does not exist.", graph.InitialState));

        if (!string.IsNullOrWhiteSpace(graph.CurrentState) && !nameCounts.ContainsKey(graph.CurrentState))
            issues.Add(Warning("MissingCurrentState", $"Current state '{graph.CurrentState}' does not exist.", graph.CurrentState));

        return issues;
    }

    private static string GetTransitionTarget(StateMachineTransitionEdge transition) =>
        transition.DisplayName ?? transition.Name ?? $"{transition.From}->{transition.To}";

    private static void AddInvalidJsonSlotIssue(ICollection<StateMachineValidationIssue> issues, JsonNode? slot, string target)
    {
        if (slot is JsonObject obj && obj.ContainsKey(InvalidJsonSlotProperty))
            issues.Add(Error("InvalidSlotJson", $"Slot '{target}' contains invalid JSON.", target));
    }

    private static StateMachineValidationIssue Error(string code, string message, string? target) =>
        new() { Severity = StateMachineValidationSeverity.Error, Code = code, Message = message, Target = target };

    private static StateMachineValidationIssue Warning(string code, string message, string? target) =>
        new() { Severity = StateMachineValidationSeverity.Warning, Code = code, Message = message, Target = target };
}
