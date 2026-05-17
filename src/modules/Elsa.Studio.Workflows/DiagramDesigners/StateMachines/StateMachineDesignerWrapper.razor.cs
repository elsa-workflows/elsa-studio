using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using static Elsa.Studio.Workflows.Designer.StateMachineDesignerConstants;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines;

/// <summary>
/// Displays and edits a StateMachine activity graph.
/// </summary>
public partial class StateMachineDesignerWrapper
{
    private StateMachineGraph? _graph;
    private IDictionary<string, ActivityStats>? _activityStats;
    private string? _selectedStateName;
    private StateMachineTransitionEdge? _selectedTransition;
    private string? _selectedActivityId;
    private string _newStateName = "";
    private string _newTransitionName = "";
    private string? _newTransitionFrom;
    private string? _newTransitionTo;
    private bool _zoomToFit = true;
    private bool _centerContent;
    private JsonObject? _loadedParameterStateMachine;
    private IDictionary<string, ActivityStats>? _loadedParameterActivityStats;

    /// <summary>
    /// Gets or sets the StateMachine activity to display.
    /// </summary>
    [Parameter] public JsonObject StateMachine { get; set; } = [];

    /// <summary>
    /// Gets or sets activity execution stats.
    /// </summary>
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the designer is read-only.
    /// </summary>
    [Parameter] public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the root activity is selected.
    /// </summary>
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the graph changes.
    /// </summary>
    [Parameter] public EventCallback GraphUpdated { get; set; }

    [CascadingParameter] private DragDropManager DragDropManager { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;
    [Inject] private IActivityNameGenerator ActivityNameGenerator { get; set; } = null!;
    [Inject] private IStateMachineMapper StateMachineMapper { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(StateMachine, _loadedParameterStateMachine) && ReferenceEquals(ActivityStats, _loadedParameterActivityStats))
            return;

        TrackParameterState(StateMachine, ActivityStats);
        LoadGraph(StateMachine, ActivityStats);
    }

    /// <summary>
    /// Loads the specified StateMachine activity.
    /// </summary>
    public Task LoadStateMachineAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStats = null)
    {
        TrackParameterState(activity, activityStats);
        LoadGraph(activity, activityStats);
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Updates the root StateMachine activity.
    /// </summary>
    public Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot update activity because the designer is read-only.");

        if (string.Equals(StateMachine.GetId(), id, StringComparison.Ordinal))
        {
            TrackParameterState(activity, _activityStats);
            LoadGraph(activity, _activityStats);
            return InvokeAsync(StateHasChanged);
        }

        if (TryUpdateSlotActivity(id, activity))
            return ApplyGraphChangesAsync();

        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Updates activity execution stats.
    /// </summary>
    public Task UpdateActivityStatsAsync(string id, ActivityStats stats)
    {
        _activityStats ??= new Dictionary<string, ActivityStats>();
        _activityStats[id] = stats;
        ActivityStats = _activityStats;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Selects the root activity or a state by name.
    /// </summary>
    public async Task SelectActivityAsync(string id)
    {
        if (string.Equals(StateMachine.GetId(), id, StringComparison.Ordinal))
        {
            _selectedStateName = null;
            _selectedTransition = null;
            await SelectRootActivityForPropertiesAsync();
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (TryFindSlotActivity(id, out var slotActivity, out var state, out var transition))
        {
            _selectedStateName = state?.Name;
            _selectedTransition = transition;
            await SelectSlotActivityForPropertiesAsync(slotActivity);

            await InvokeAsync(StateHasChanged);
            return;
        }

        _selectedStateName = _graph?.States.FirstOrDefault(x => string.Equals(x.Name, id, StringComparison.Ordinal))?.Name;
        _selectedTransition = null;
        if (_selectedStateName != null)
            await SelectRootActivityForPropertiesAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Reads the root StateMachine activity.
    /// </summary>
    public Task<JsonObject> ReadRootActivityAsync()
    {
        if (HasValidationErrors())
            throw new InvalidOperationException("Cannot read the StateMachine activity because the graph has validation errors.");

        return Task.FromResult(_graph != null ? StateMachineMapper.Map(_graph) : StateMachine);
    }

    /// <summary>
    /// Applies the fit layout density.
    /// </summary>
    public Task ZoomToFitAsync()
    {
        _zoomToFit = true;
        _centerContent = false;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Centers the graph content.
    /// </summary>
    public Task CenterContentAsync()
    {
        _zoomToFit = false;
        _centerContent = true;
        return InvokeAsync(StateHasChanged);
    }

    private void LoadGraph(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        var selectedTransition = _selectedTransition;
        var selectedTransitionIdentityIndex = GetTransitionIdentityIndex(_graph, selectedTransition);
        var newTransitionFrom = _newTransitionFrom;
        var newTransitionTo = _newTransitionTo;
        StateMachine = activity;
        ActivityStats = activityStats;
        _activityStats = activityStats;
        _graph = StateMachineMapper.Map(activity);

        var stateNames = _graph.States.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        _newTransitionFrom = !string.IsNullOrWhiteSpace(newTransitionFrom) && stateNames.Contains(newTransitionFrom) ? newTransitionFrom : _graph.States.FirstOrDefault()?.Name;
        _newTransitionTo = !string.IsNullOrWhiteSpace(newTransitionTo) && stateNames.Contains(newTransitionTo) ? newTransitionTo : _graph.States.Skip(1).FirstOrDefault()?.Name ?? _newTransitionFrom;
        _selectedTransition = selectedTransition != null
            ? FindSameTransition(_graph, selectedTransition, selectedTransitionIdentityIndex)
            : null;
    }

    private async Task SelectStateAsync(StateMachineStateNode state)
    {
        _selectedStateName = state.Name;
        _selectedTransition = null;
        await SelectRootActivityForPropertiesAsync();
    }

    private async Task SelectTransitionAsync(StateMachineTransitionEdge transition)
    {
        _selectedTransition = transition;
        _selectedStateName = null;
        await SelectRootActivityForPropertiesAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddStateAsync()
    {
        if (IsReadOnly || _graph == null)
            return;

        var stateName = GetUniqueStateName(_graph, _newStateName);
        var state = new StateMachineStateNode
        {
            Name = stateName,
            Source = new JsonObject { ["name"] = stateName }
        };

        _graph.States.Add(state);
        _graph.InitialState ??= stateName;
        _graph.CurrentState ??= stateName;
        _newStateName = "";
        _selectedStateName = stateName;
        _selectedTransition = null;
        await ApplyGraphChangesAsync();
    }

    private async Task DeleteSelectedStateAsync()
    {
        if (IsReadOnly || _graph == null || SelectedState == null)
            return;

        var stateName = SelectedState.Name;
        _graph.States.Remove(SelectedState);

        foreach (var transition in _graph.Transitions.Where(x =>
                     string.Equals(x.From, stateName, StringComparison.Ordinal) ||
                     string.Equals(x.To, stateName, StringComparison.Ordinal)).ToList())
        {
            _graph.Transitions.Remove(transition);
        }

        if (string.Equals(_graph.InitialState, stateName, StringComparison.Ordinal))
            _graph.InitialState = _graph.States.FirstOrDefault()?.Name;

        if (string.Equals(_graph.CurrentState, stateName, StringComparison.Ordinal))
            _graph.CurrentState = _graph.States.FirstOrDefault()?.Name;

        _selectedStateName = _graph.States.FirstOrDefault()?.Name;
        await ApplyGraphChangesAsync();
    }

    private async Task RenameSelectedStateAsync(ChangeEventArgs e)
    {
        var selectedState = SelectedState;
        if (IsReadOnly || _graph == null || selectedState == null)
            return;

        var oldName = selectedState.Name;
        var newName = GetUniqueStateName(_graph, e.Value?.ToString() ?? "", selectedState);

        if (string.Equals(oldName, newName, StringComparison.Ordinal))
            return;

        selectedState.Name = newName;
        _selectedStateName = newName;

        foreach (var transition in _graph.Transitions)
        {
            if (string.Equals(transition.From, oldName, StringComparison.Ordinal))
                transition.From = newName;

            if (string.Equals(transition.To, oldName, StringComparison.Ordinal))
                transition.To = newName;
        }

        if (string.Equals(_graph.InitialState, oldName, StringComparison.Ordinal))
            _graph.InitialState = newName;

        if (string.Equals(_graph.CurrentState, oldName, StringComparison.Ordinal))
            _graph.CurrentState = newName;

        await ApplyGraphChangesAsync();
    }

    private async Task AddTransitionAsync()
    {
        if (IsReadOnly || _graph == null || string.IsNullOrWhiteSpace(_newTransitionFrom) || string.IsNullOrWhiteSpace(_newTransitionTo))
            return;

        var transitionName = GetUniqueTransitionName(_graph, _newTransitionName);

        var transition = new StateMachineTransitionEdge
        {
            Name = transitionName,
            DisplayName = transitionName,
            From = _newTransitionFrom,
            To = _newTransitionTo,
            Source = new JsonObject
            {
                ["name"] = transitionName,
                ["displayName"] = transitionName,
                ["from"] = _newTransitionFrom,
                ["to"] = _newTransitionTo
            }
        };

        _graph.Transitions.Add(transition);
        _selectedTransition = transition;
        _selectedStateName = null;
        _newTransitionName = "";
        await ApplyGraphChangesAsync();
    }

    private async Task DeleteSelectedTransitionAsync()
    {
        if (IsReadOnly || _graph == null || _selectedTransition == null)
            return;

        _graph.Transitions.Remove(_selectedTransition);
        _selectedTransition = _graph.Transitions.FirstOrDefault();
        await ApplyGraphChangesAsync();
    }

    private async Task SetInitialStateAsync(ChangeEventArgs e)
    {
        if (_graph == null || IsReadOnly)
            return;

        _graph.InitialState = NormalizeOptionalStateName(e.Value);
        await ApplyGraphChangesAsync();
    }

    private async Task SetCurrentStateAsync(ChangeEventArgs e)
    {
        if (_graph == null || IsReadOnly)
            return;

        _graph.CurrentState = NormalizeOptionalStateName(e.Value);
        await ApplyGraphChangesAsync();
    }

    private async Task SetTransitionFromAsync(ChangeEventArgs e)
    {
        if (_selectedTransition == null || IsReadOnly)
            return;

        _selectedTransition.From = e.Value?.ToString() ?? "";
        await ApplyGraphChangesAsync();
    }

    private async Task SetTransitionToAsync(ChangeEventArgs e)
    {
        if (_selectedTransition == null || IsReadOnly)
            return;

        _selectedTransition.To = e.Value?.ToString() ?? "";
        await ApplyGraphChangesAsync();
    }

    private async Task SetTransitionNameAsync(ChangeEventArgs e)
    {
        if (_selectedTransition == null || IsReadOnly || _graph == null)
            return;

        _selectedTransition.Name = GetUniqueTransitionName(_graph, e.Value?.ToString(), _selectedTransition, true);
        await ApplyGraphChangesAsync();
    }

    private async Task SetTransitionDisplayNameAsync(ChangeEventArgs e)
    {
        if (_selectedTransition == null || IsReadOnly)
            return;

        _selectedTransition.DisplayName = NormalizeOptionalString(e.Value);
        await ApplyGraphChangesAsync();
    }

    private async Task SetStateSlotAsync(string slotName, ChangeEventArgs e)
    {
        if (SelectedState == null || IsReadOnly)
            return;

        var refreshSelectedActivity = IsSelectedSlotActivity(GetStateSlot(SelectedState, slotName));
        var node = ParseJsonSlot(e.Value?.ToString(), slotName);
        SetStateSlot(SelectedState, slotName, node);

        await ApplyGraphChangesAndRefreshSelectionAsync(refreshSelectedActivity, () => SelectedState != null ? GetStateSlot(SelectedState, slotName) : null);
    }

    private async Task SetTransitionSlotAsync(string slotName, ChangeEventArgs e)
    {
        if (_selectedTransition == null || IsReadOnly)
            return;

        var value = e.Value?.ToString();
        var refreshSelectedActivity = IsSelectedSlotActivity(GetTransitionSlot(_selectedTransition, slotName));

        switch (slotName)
        {
            case "trigger":
                _selectedTransition.Trigger = ParseJsonSlot(value, slotName);
                break;
            case "condition":
                _selectedTransition.Condition = ParseJsonSlot(value, slotName);
                break;
            case "action":
                _selectedTransition.Action = ParseJsonSlot(value, slotName);
                break;
        }

        await ApplyGraphChangesAndRefreshSelectionAsync(refreshSelectedActivity, () => _selectedTransition != null ? GetTransitionSlot(_selectedTransition, slotName) : null);
    }

    private async Task ClearStateSlotAsync(string slotName)
    {
        if (SelectedState == null || IsReadOnly)
            return;

        var refreshSelectedActivity = IsSelectedSlotActivity(GetStateSlot(SelectedState, slotName));
        SetStateSlot(SelectedState, slotName, null);

        await ApplyGraphChangesAndRefreshSelectionAsync(refreshSelectedActivity, () => null);
    }

    private async Task ClearTransitionSlotAsync(string slotName)
    {
        if (_selectedTransition == null || IsReadOnly)
            return;

        var refreshSelectedActivity = IsSelectedSlotActivity(GetTransitionSlot(_selectedTransition, slotName));
        switch (slotName)
        {
            case "trigger":
                _selectedTransition.Trigger = null;
                break;
            case "condition":
                _selectedTransition.Condition = null;
                break;
            case "action":
                _selectedTransition.Action = null;
                break;
        }

        await ApplyGraphChangesAndRefreshSelectionAsync(refreshSelectedActivity, () => null);
    }

    private void OnSlotDragOver(DragEventArgs e)
    {
        e.DataTransfer.DropEffect = !IsReadOnly && DragDropManager.Payload is ActivityDescriptor ? "move" : "none";
    }

    private async Task OnStateSlotDropAsync(string slotName)
    {
        if (IsReadOnly || SelectedState == null || DragDropManager.Payload is not ActivityDescriptor descriptor)
            return;

        var activity = CreateSlotActivity(descriptor);

        SetStateSlot(SelectedState, slotName, activity);

        DragDropManager.Payload = null;
        await ApplyGraphChangesAndRefreshSelectionAsync(true, () => SelectedState != null ? GetStateSlot(SelectedState, slotName) : null);
    }

    private async Task OnTransitionSlotDropAsync(string slotName)
    {
        if (IsReadOnly || _selectedTransition == null || DragDropManager.Payload is not ActivityDescriptor descriptor)
            return;

        var activity = CreateSlotActivity(descriptor);

        switch (slotName)
        {
            case "trigger":
                _selectedTransition.Trigger = activity;
                break;
            case "action":
                _selectedTransition.Action = activity;
                break;
        }

        DragDropManager.Payload = null;
        await ApplyGraphChangesAndRefreshSelectionAsync(true, () => _selectedTransition != null ? GetTransitionSlot(_selectedTransition, slotName) : null);
    }

    private async Task ApplyGraphChangesAndRefreshSelectionAsync(bool refreshSelectedActivity, Func<JsonNode?> getReplacementSlot)
    {
        await ApplyGraphChangesAsync();

        if (!refreshSelectedActivity)
            return;

        if (getReplacementSlot() is JsonObject replacementActivity && replacementActivity.IsActivity())
            await SelectSlotActivityForPropertiesAsync(replacementActivity);
        else
            await SelectRootActivityForPropertiesAsync();
    }

    private async Task ApplyGraphChangesAsync()
    {
        if (_graph == null)
            return;

        if (HasStructuralValidationErrors())
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        var selectedStateName = _selectedStateName;
        var activity = StateMachineMapper.Map(_graph);
        LoadGraph(activity, _activityStats);
        _selectedStateName = selectedStateName;

        if (!HasValidationErrors() && GraphUpdated.HasDelegate)
            await GraphUpdated.InvokeAsync();

        await InvokeAsync(StateHasChanged);
    }

    private string GetCanvasClass() =>
        _zoomToFit ? "state-machine-designer__canvas state-machine-designer__canvas--fit" : "state-machine-designer__canvas";

    private string GetStateClass(StateMachineStateNode state)
    {
        var className = "state-machine-designer__state";

        if (string.Equals(state.Name, _selectedStateName, StringComparison.Ordinal))
            className += " state-machine-designer__state--selected";

        if (state.IsTerminal)
            className += " state-machine-designer__state--terminal";

        return className;
    }

    private string GetTransitionClass(StateMachineTransitionEdge transition)
    {
        var className = "state-machine-designer__transition";

        if (_selectedTransition != null && IsSameTransition(transition, _selectedTransition))
            className += " state-machine-designer__transition--selected";

        if (string.IsNullOrWhiteSpace(transition.From) || string.IsNullOrWhiteSpace(transition.To))
            className += " state-machine-designer__transition--invalid";

        return className;
    }

    private static string GetIssueClass(StateMachineValidationIssue issue) =>
        issue.Severity == StateMachineValidationSeverity.Error
            ? "state-machine-designer__issue state-machine-designer__issue--error"
            : "state-machine-designer__issue state-machine-designer__issue--warning";

    private static string DisplayValue(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string GetTransitionDisplayText(StateMachineTransitionEdge transition) =>
        DisplayValue(NormalizeOptionalString(transition.DisplayName) ?? transition.Name);

    private IEnumerable<StateMachineTransitionEdge> GetOutgoingTransitions(StateMachineStateNode state) =>
        _graph?.Transitions.Where(x => string.Equals(x.From, state.Name, StringComparison.Ordinal)) ?? [];

    private StateMachineStateNode? SelectedState =>
        _graph?.States.FirstOrDefault(x => string.Equals(x.Name, _selectedStateName, StringComparison.Ordinal));

    private async Task SelectRootActivityForPropertiesAsync()
    {
        _selectedActivityId = GetActivitySelectionId(StateMachine);

        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(StateMachine);
    }

    private async Task SelectSlotActivityForPropertiesAsync(JsonObject activity)
    {
        _selectedActivityId = GetActivitySelectionId(activity);

        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(activity);
    }

    private bool IsSelectedSlotActivity(JsonNode? slot) =>
        slot is JsonObject activity &&
        _selectedActivityId != null &&
        (string.Equals(activity.GetId(), _selectedActivityId, StringComparison.Ordinal) ||
         string.Equals(activity.GetNodeId(), _selectedActivityId, StringComparison.Ordinal));

    private static string? GetActivitySelectionId(JsonObject activity) =>
        NormalizeOptionalString(activity.GetId()) ?? NormalizeOptionalString(activity.GetNodeId());

    private static JsonNode? GetStateSlot(StateMachineStateNode state, string slotName) =>
        slotName == "entry" ? state.Entry : state.Exit;

    private static void SetStateSlot(StateMachineStateNode state, string slotName, JsonNode? slot)
    {
        if (slotName == "entry")
            state.Entry = slot;
        else
            state.Exit = slot;
    }

    private static JsonNode? GetTransitionSlot(StateMachineTransitionEdge transition, string slotName) =>
        slotName switch
        {
            "trigger" => transition.Trigger,
            "condition" => transition.Condition,
            "action" => transition.Action,
            _ => null
        };

    private static string GetUniqueStateName(StateMachineGraph graph, string requestedName, StateMachineStateNode? excludedState = null)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedName) ? "State" : requestedName.Trim();
        var stateName = baseName;
        var index = 2;
        var names = graph.States
            .Where(x => !ReferenceEquals(x, excludedState))
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        while (names.Contains(stateName))
            stateName = $"{baseName}{index++}";

        return stateName;
    }

    private static string? GetUniqueTransitionName(StateMachineGraph graph, string? requestedName = null, StateMachineTransitionEdge? excludedTransition = null, bool allowNull = false)
    {
        var names = graph.Transitions
            .Where(x => !ReferenceEquals(x, excludedTransition))
            .Select(x => x.Name)
            .Where(x => x != null)
            .Select(x => x!)
            .ToHashSet(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            var baseName = requestedName.Trim();
            var name = baseName;
            var suffix = 2;

            while (names.Contains(name))
                name = $"{baseName}{suffix++}";

            return name;
        }

        if (allowNull)
            return null;

        var index = graph.Transitions.Count + 1;
        var defaultName = $"Transition{index}";

        while (names.Contains(defaultName))
            defaultName = $"Transition{++index}";

        return defaultName;
    }

    private static bool IsSameTransition(StateMachineTransitionEdge left, StateMachineTransitionEdge right) =>
        string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
        string.Equals(left.From, right.From, StringComparison.Ordinal) &&
        string.Equals(left.To, right.To, StringComparison.Ordinal);

    private static int? GetTransitionIdentityIndex(StateMachineGraph? graph, StateMachineTransitionEdge? transition)
    {
        if (graph == null || transition == null)
            return null;

        int? firstMatchIndex = null;
        var identityIndex = 0;

        foreach (var candidate in graph.Transitions.Where(x => IsSameTransition(x, transition)))
        {
            firstMatchIndex ??= identityIndex;

            if (ReferenceEquals(candidate, transition))
                return identityIndex;

            identityIndex++;
        }

        return firstMatchIndex;
    }

    private static StateMachineTransitionEdge? FindSameTransition(StateMachineGraph graph, StateMachineTransitionEdge transition, int? identityIndex)
    {
        var matches = graph.Transitions.Where(x => IsSameTransition(x, transition)).ToList();

        if (identityIndex is { } index && index >= 0 && index < matches.Count)
            return matches[index];

        return matches.FirstOrDefault();
    }

    private static JsonNode? ParseJsonSlot(string? value, string slotName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return JsonNode.Parse(value);
        }
        catch
        {
            return new JsonObject
            {
                [InvalidJsonSlotProperty] = true,
                ["slot"] = slotName,
                [InvalidJsonSlotSourceProperty] = value
            };
        }
    }

    private static string FormatJsonSlot(JsonNode? node) =>
        node is JsonObject obj && obj.ContainsKey(InvalidJsonSlotProperty)
            ? obj[InvalidJsonSlotSourceProperty]?.GetValue<string>() ?? ""
            : node?.ToJsonString(new() { WriteIndented = true }) ?? "";

    private JsonObject CreateSlotActivity(ActivityDescriptor descriptor)
    {
        var activityId = IdentityGenerator.GenerateId();
        var activities = GetIndexedSlotActivities().ToList();
        var activity = new JsonObject
        {
            ["id"] = activityId,
            ["nodeId"] = $"{StateMachine.GetNodeId()}:{activityId}",
            ["name"] = ActivityNameGenerator.GenerateNextName(activities, descriptor),
            ["type"] = descriptor.TypeName,
            ["version"] = descriptor.Version
        };

        foreach (var property in descriptor.ConstructionProperties)
        {
            var valueNode = JsonSerializer.SerializeToNode(property.Value);
            activity.SetProperty(valueNode, property.Key.Camelize());
        }

        return activity;
    }

    private void TrackParameterState(JsonObject activity, IDictionary<string, ActivityStats>? activityStats)
    {
        _loadedParameterStateMachine = activity;
        _loadedParameterActivityStats = activityStats;
    }

    private bool HasValidationErrors() =>
        _graph?.ValidationIssues.Any(x => x.Severity == StateMachineValidationSeverity.Error) == true;

    private bool HasStructuralValidationErrors() =>
        _graph?.ValidationIssues.Any(x =>
            x.Severity == StateMachineValidationSeverity.Error &&
            x.Code is "InvalidStateCollection" or "InvalidTransitionCollection" or "InvalidStateItem" or "InvalidTransitionItem") == true;

    private IEnumerable<JsonObject> GetIndexedSlotActivities()
    {
        if (_graph == null)
            yield break;

        var slots = _graph.States.SelectMany(x => new[] { x.Entry, x.Exit })
            .Concat(_graph.Transitions.SelectMany(x => new[] { x.Trigger, x.Action }));

        foreach (var slot in slots)
        {
            if (slot is JsonObject activity && activity.IsActivity())
                yield return activity;
        }
    }

    private bool TryFindSlotActivity(
        string id,
        out JsonObject activity,
        out StateMachineStateNode? state,
        out StateMachineTransitionEdge? transition)
    {
        if (_graph != null)
        {
            foreach (var candidateState in _graph.States)
            {
                if (TryMatchSlotActivity(candidateState.Entry, id, out activity))
                {
                    state = candidateState;
                    transition = null;
                    return true;
                }

                if (TryMatchSlotActivity(candidateState.Exit, id, out activity))
                {
                    state = candidateState;
                    transition = null;
                    return true;
                }
            }

            foreach (var candidateTransition in _graph.Transitions)
            {
                if (TryMatchSlotActivity(candidateTransition.Trigger, id, out activity) ||
                    TryMatchSlotActivity(candidateTransition.Action, id, out activity))
                {
                    state = null;
                    transition = candidateTransition;
                    return true;
                }
            }
        }

        activity = [];
        state = null;
        transition = null;
        return false;
    }

    private bool TryUpdateSlotActivity(string id, JsonObject activity)
    {
        if (_graph == null)
            return false;

        foreach (var state in _graph.States)
        {
            if (TryMatchSlotActivity(state.Entry, id, out _))
            {
                state.Entry = activity.DeepClone();
                return true;
            }

            if (TryMatchSlotActivity(state.Exit, id, out _))
            {
                state.Exit = activity.DeepClone();
                return true;
            }
        }

        foreach (var transition in _graph.Transitions)
        {
            if (TryMatchSlotActivity(transition.Trigger, id, out _))
            {
                transition.Trigger = activity.DeepClone();
                return true;
            }

            if (TryMatchSlotActivity(transition.Action, id, out _))
            {
                transition.Action = activity.DeepClone();
                return true;
            }
        }

        return false;
    }

    private static bool TryMatchSlotActivity(JsonNode? slot, string id, out JsonObject activity)
    {
        if (slot is JsonObject obj && obj.IsActivity() &&
            (string.Equals(obj.GetId(), id, StringComparison.Ordinal) ||
             string.Equals(obj.GetNodeId(), id, StringComparison.Ordinal)))
        {
            activity = obj;
            return true;
        }

        activity = [];
        return false;
    }

    private static string? NormalizeOptionalStateName(object? value)
    {
        var stateName = value?.ToString();
        return string.IsNullOrWhiteSpace(stateName) ? null : stateName;
    }

    private static string? NormalizeOptionalString(object? value)
    {
        var text = value?.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
