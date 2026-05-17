using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Services;

/// <summary>
/// Maps StateMachine activity JSON to and from a graph representation.
/// </summary>
public class StateMachineMapper(StateMachineValidator validator) : IStateMachineMapper
{
    /// <inheritdoc />
    public StateMachineGraph Map(JsonObject activity)
    {
        var graph = new StateMachineGraph
        {
            Activity = CloneObject(activity),
            InitialState = GetString(activity, "initialState"),
            CurrentState = GetString(activity, "currentState")
        };

        foreach (var stateObject in GetObjectArray(activity, "states"))
        {
            graph.States.Add(new StateMachineStateNode
            {
                Source = stateObject,
                Name = GetString(stateObject, "name") ?? "",
                Entry = CloneNode(stateObject["entry"]),
                Exit = CloneNode(stateObject["exit"])
            });
        }

        foreach (var transitionObject in GetObjectArray(activity, "transitions"))
        {
            graph.Transitions.Add(new StateMachineTransitionEdge
            {
                Source = transitionObject,
                Name = GetString(transitionObject, "name"),
                DisplayName = GetString(transitionObject, "displayName"),
                From = GetString(transitionObject, "from") ?? "",
                To = GetString(transitionObject, "to") ?? "",
                Trigger = CloneNode(transitionObject["trigger"]),
                Condition = CloneNode(transitionObject["condition"]),
                Action = CloneNode(transitionObject["action"])
            });
        }

        AddArrayItemIssues(graph, activity, "states", "InvalidStateItem", "StateMachine states must be JSON objects.");
        AddArrayItemIssues(graph, activity, "transitions", "InvalidTransitionItem", "StateMachine transitions must be JSON objects.");
        UpdateTerminalStateMarkers(graph);
        AddValidationIssues(graph);
        return graph;
    }

    /// <inheritdoc />
    public JsonObject Map(StateMachineGraph graph)
    {
        var activity = CloneObject(graph.Activity);
        SetOptionalStringOrRemove(activity, "initialState", graph.InitialState);
        SetOptionalStringOrRemove(activity, "currentState", graph.CurrentState);

        activity["states"] = new JsonArray(graph.States.Select(MapState).ToArray<JsonNode?>());
        activity["transitions"] = new JsonArray(graph.Transitions.Select(MapTransition).ToArray<JsonNode?>());

        return activity;
    }

    private static JsonObject MapState(StateMachineStateNode state)
    {
        var stateObject = CloneObject(state.Source);
        SetOrRemove(stateObject, "name", state.Name);
        SetOrRemove(stateObject, "entry", state.Entry);
        SetOrRemove(stateObject, "exit", state.Exit);
        return stateObject;
    }

    private static JsonObject MapTransition(StateMachineTransitionEdge transition)
    {
        var transitionObject = CloneObject(transition.Source);
        SetOrRemove(transitionObject, "name", transition.Name);
        SetOrRemove(transitionObject, "displayName", transition.DisplayName);
        SetOrRemove(transitionObject, "from", transition.From);
        SetOrRemove(transitionObject, "to", transition.To);
        SetOrRemove(transitionObject, "trigger", transition.Trigger);
        SetOrRemove(transitionObject, "condition", transition.Condition);
        SetOrRemove(transitionObject, "action", transition.Action);
        return transitionObject;
    }

    private void AddValidationIssues(StateMachineGraph graph)
    {
        foreach (var issue in validator.Validate(graph))
            graph.ValidationIssues.Add(issue);
    }

    private static void AddArrayItemIssues(StateMachineGraph graph, JsonObject activity, string propertyName, string code, string message)
    {
        if (activity[propertyName] is not JsonArray array)
            return;

        for (var index = 0; index < array.Count; index++)
        {
            if (array[index] is JsonObject)
                continue;

            graph.ValidationIssues.Add(new()
            {
                Severity = StateMachineValidationSeverity.Error,
                Code = code,
                Message = message,
                Target = $"{propertyName}[{index}]"
            });
        }
    }

    private static void UpdateTerminalStateMarkers(StateMachineGraph graph)
    {
        var validStateNames = graph.States.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);

        foreach (var state in graph.States)
        {
            state.IsTerminal = !graph.Transitions.Any(x =>
                string.Equals(x.From, state.Name, StringComparison.Ordinal) &&
                validStateNames.Contains(x.To));
        }
    }

    private static IEnumerable<JsonObject> GetObjectArray(JsonObject activity, string propertyName)
    {
        if (activity[propertyName] is not JsonArray array)
            yield break;

        foreach (var item in array.OfType<JsonObject>())
            yield return CloneObject(item);
    }

    private static string? GetString(JsonObject source, string propertyName)
    {
        var node = source[propertyName];

        if (node == null)
            return null;

        return node is JsonValue value && value.TryGetValue<string>(out var text) ? text : node.ToString();
    }

    private static JsonObject CloneObject(JsonObject source) =>
        (JsonObject)source.DeepClone();

    private static JsonNode? CloneNode(JsonNode? source) =>
        source?.DeepClone();

    private static void SetOrRemove(JsonObject target, string propertyName, string? value)
    {
        if (value == null)
        {
            target.Remove(propertyName);
            return;
        }

        target[propertyName] = value;
    }

    private static void SetOptionalStringOrRemove(JsonObject target, string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            target.Remove(propertyName);
            return;
        }

        target[propertyName] = value;
    }

    private static void SetOrRemove(JsonObject target, string propertyName, JsonNode? value)
    {
        if (value == null)
        {
            target.Remove(propertyName);
            return;
        }

        target[propertyName] = value.DeepClone();
    }
}
