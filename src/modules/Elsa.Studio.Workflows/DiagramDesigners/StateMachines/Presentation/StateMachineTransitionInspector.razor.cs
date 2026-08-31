using System.Text.Json.Nodes;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.Designer.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

public partial class StateMachineTransitionInspector
{
    private readonly string _id = $"state-machine-transition-inspector-{Guid.NewGuid():N}";

    [Parameter] public StateMachineTransitionEdge? Transition { get; set; }
    [Parameter] public IReadOnlyCollection<StateMachineStateNode> States { get; set; } = [];
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public IReadOnlyCollection<StateMachineValidationIssue> ValidationIssues { get; set; } = [];
    [Parameter] public EventCallback<string?> NameChanged { get; set; }
    [Parameter] public EventCallback<string?> DisplayNameChanged { get; set; }
    [Parameter] public EventCallback<string> FromChanged { get; set; }
    [Parameter] public EventCallback<string> ToChanged { get; set; }

    // Retained for compatibility with the original wrapper contract. New callers should use
    // the semantic activity events below so the wrapper can choose the appropriate mutation path.
    [Parameter] public EventCallback<StateMachineSlotValueChange> SlotChanged { get; set; }
    [Parameter] public EventCallback<string> SlotCleared { get; set; }
    [Parameter] public EventCallback<string> SlotDropRequested { get; set; }

    [Parameter] public EventCallback<string> ActivityAddRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityOpenRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityReplaceRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityClearRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityDropRequested { get; set; }
    [Parameter] public EventCallback<string> ConditionEditRequested { get; set; }
    [Parameter] public EventCallback DeleteRequested { get; set; }

    private string HeadingId => $"{_id}-heading";
    private string TriggerStageId => $"{_id}-trigger-stage";
    private string ConditionStageId => $"{_id}-condition-stage";
    private string ActionStageId => $"{_id}-action-stage";
    private string DestinationStageId => $"{_id}-destination-stage";
    private string NameId => $"{_id}-name";
    private string DisplayNameId => $"{_id}-display-name";
    private string FromId => $"{_id}-from";
    private string ToId => $"{_id}-to";

    private Task ChangeNameAsync(ChangeEventArgs args) => NameChanged.InvokeAsync(NormalizeOptionalValue(args.Value));
    private Task ChangeDisplayNameAsync(ChangeEventArgs args) => DisplayNameChanged.InvokeAsync(NormalizeOptionalValue(args.Value));
    private Task ChangeFromAsync(ChangeEventArgs args) => FromChanged.InvokeAsync(args.Value?.ToString() ?? "");
    private Task ChangeToAsync(ChangeEventArgs args) => ToChanged.InvokeAsync(args.Value?.ToString() ?? "");

    private Task RequestConditionEditAsync() => ConditionEditRequested.InvokeAsync("condition");

    private Task RequestActivityAddAsync(string slotName) => ActivityAddRequested.InvokeAsync(slotName);
    private Task RequestActivityOpenAsync(string slotName) => ActivityOpenRequested.InvokeAsync(slotName);
    private Task RequestActivityReplaceAsync(string slotName) => ActivityReplaceRequested.InvokeAsync(slotName);

    private Task RequestActivityClearAsync(string slotName) => ActivityClearRequested.HasDelegate
        ? ActivityClearRequested.InvokeAsync(slotName)
        : SlotCleared.InvokeAsync(slotName);

    private Task RequestActivityDropAsync(string slotName) => ActivityDropRequested.HasDelegate
        ? ActivityDropRequested.InvokeAsync(slotName)
        : SlotDropRequested.InvokeAsync(slotName);

    private static string? NormalizeOptionalValue(object? value)
    {
        var text = value?.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private RenderFragment RenderActivitySlot(string slotName, JsonNode? value, bool isTrigger) => builder =>
    {
        var activity = DescribeActivity(value);
        var sequence = 0;

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", "state-machine-transition-inspector__slot");
        builder.AddAttribute(sequence++, "data-slot-state", activity.State.ToString().ToLowerInvariant());
        builder.AddAttribute(sequence++, "data-slot-action", "drop");
        builder.AddAttribute(sequence++, "aria-label", Localizer[$"{slotName} activity slot"]);

        if (!IsReadOnly)
        {
            builder.AddAttribute(sequence++, "ondragover", EventCallback.Factory.Create<DragEventArgs>(this, OnDragOverAsync));
            builder.AddEventPreventDefaultAttribute(sequence++, "ondragover", true);
            builder.AddAttribute(sequence++, "ondrop", EventCallback.Factory.Create<DragEventArgs>(this, _ => RequestActivityDropAsync(slotName)));
            builder.AddEventPreventDefaultAttribute(sequence++, "ondrop", true);
        }

        if (activity.State == ActivityState.Empty)
        {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "state-machine-transition-inspector__slot-empty");
            builder.AddAttribute(sequence++, "data-testid", $"state-machine-transition-{slotName}-empty");
            builder.OpenElement(sequence++, "strong");
            builder.AddContent(sequence++, isTrigger ? Localizer["No trigger configured"] : Localizer["No action configured"]);
            builder.CloseElement();
            builder.OpenElement(sequence++, "span");
            builder.AddContent(sequence++, isTrigger
                ? Localizer["This transition evaluates immediately after source entry."]
                : Localizer["No activity runs between source exit and the target state."]);
            builder.CloseElement();
            if (!IsReadOnly)
                AddSlotButton(builder, ref sequence, "add", isTrigger ? Localizer["Add trigger"] : Localizer["Add action"], $"Add {slotName} activity", slotName, RequestActivityAddAsync);
            builder.CloseElement();
        }
        else
        {
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", activity.State == ActivityState.Malformed
                ? "state-machine-transition-inspector__activity-card state-machine-transition-inspector__activity-card--invalid"
                : "state-machine-transition-inspector__activity-card");
            builder.AddAttribute(sequence++, "data-testid", $"state-machine-transition-{slotName}-activity");
            builder.AddAttribute(sequence++, "data-activity-state", activity.State.ToString().ToLowerInvariant());

            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "state-machine-transition-inspector__activity-copy");
            builder.OpenElement(sequence++, "strong");
            builder.AddContent(sequence++, activity.Title);
            builder.CloseElement();
            builder.OpenElement(sequence++, "span");
            builder.AddContent(sequence++, activity.Detail);
            builder.CloseElement();
            builder.CloseElement();

            if (activity.State == ActivityState.Malformed)
            {
                builder.OpenElement(sequence++, "p");
                builder.AddAttribute(sequence++, "class", "state-machine-transition-inspector__slot-warning");
                builder.AddAttribute(sequence++, "role", "status");
                builder.AddContent(sequence++, Localizer["This definition is invalid and will be preserved until you replace or clear it."]);
                builder.CloseElement();
            }

            if (!string.IsNullOrWhiteSpace(activity.RawDefinition))
            {
                builder.OpenElement(sequence++, "details");
                builder.AddAttribute(sequence++, "class", "state-machine-transition-inspector__definition");
                builder.OpenElement(sequence++, "summary");
                builder.AddContent(sequence++, Localizer["View definition"]);
                builder.CloseElement();
                builder.OpenElement(sequence++, "pre");
                builder.AddAttribute(sequence++, "data-testid", $"state-machine-transition-{slotName}-definition");
                builder.AddContent(sequence++, activity.RawDefinition);
                builder.CloseElement();
                builder.CloseElement();
            }

            if (!IsReadOnly)
            {
                builder.OpenElement(sequence++, "div");
                builder.AddAttribute(sequence++, "class", "state-machine-transition-inspector__slot-actions");
                if (activity.State != ActivityState.Malformed)
                    AddSlotButton(builder, ref sequence, "open", Localizer["Open"], $"Open {slotName} activity", slotName, RequestActivityOpenAsync);
                AddSlotButton(builder, ref sequence, "replace", Localizer["Replace"], $"Replace {slotName} activity", slotName, RequestActivityReplaceAsync);
                AddSlotButton(builder, ref sequence, "clear", Localizer["Clear"], $"Clear {slotName} activity", slotName, RequestActivityClearAsync);
                builder.CloseElement();
            }

            builder.CloseElement();
        }

        builder.CloseElement();
    };

    private RenderFragment RenderCondition(JsonNode? value) => builder =>
    {
        var condition = DescribeCondition(value);
        var sequence = 0;

        builder.OpenElement(sequence++, "div");
        builder.AddAttribute(sequence++, "class", condition.State is ConditionState.Never or ConditionState.Malformed
            ? "state-machine-transition-inspector__condition state-machine-transition-inspector__condition--warning"
            : "state-machine-transition-inspector__condition");
        builder.AddAttribute(sequence++, "data-condition-state", condition.State.ToString().ToLowerInvariant());
        builder.AddAttribute(sequence++, "data-testid", "state-machine-transition-condition-summary");
        builder.OpenElement(sequence++, "strong");
        builder.AddContent(sequence++, condition.Title);
        builder.CloseElement();
        builder.OpenElement(sequence++, "span");
        builder.AddContent(sequence++, condition.Detail);
        builder.CloseElement();

        if (!string.IsNullOrWhiteSpace(condition.Warning))
        {
            builder.OpenElement(sequence++, "p");
            builder.AddAttribute(sequence++, "class", "state-machine-transition-inspector__slot-warning");
            builder.AddAttribute(sequence++, "role", "status");
            builder.AddContent(sequence++, condition.Warning);
            builder.CloseElement();
        }

        if (!string.IsNullOrWhiteSpace(condition.RawDefinition))
        {
            builder.OpenElement(sequence++, "details");
            builder.AddAttribute(sequence++, "class", "state-machine-transition-inspector__definition");
            builder.OpenElement(sequence++, "summary");
            builder.AddContent(sequence++, Localizer["View definition"]);
            builder.CloseElement();
            builder.OpenElement(sequence++, "pre");
            builder.AddAttribute(sequence++, "data-testid", "state-machine-transition-condition-definition");
            builder.AddContent(sequence++, condition.RawDefinition);
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    };

    private void AddSlotButton(
        RenderTreeBuilder builder,
        ref int sequence,
        string action,
        string text,
        string ariaLabel,
        string slotName,
        Func<string, Task> callback)
    {
        builder.OpenElement(sequence++, "button");
        builder.AddAttribute(sequence++, "type", "button");
        builder.AddAttribute(sequence++, "class", action == "clear"
            ? "state-machine-transition-inspector__button state-machine-transition-inspector__button--subtle"
            : "state-machine-transition-inspector__button");
        builder.AddAttribute(sequence++, "data-slot-action", action);
        builder.AddAttribute(sequence++, "aria-label", Localizer[ariaLabel]);
        builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create(this, () => callback(slotName)));
        builder.AddContent(sequence++, text);
        builder.CloseElement();
    }

    private static Task OnDragOverAsync(DragEventArgs _) => Task.CompletedTask;

    private ActivityPresentation DescribeActivity(JsonNode? value)
    {
        if (value == null)
            return new(ActivityState.Empty, string.Empty, string.Empty, string.Empty);

        var rawDefinition = StateMachinePresentationFormatter.JsonSlot(value);
        if (value is not JsonObject obj)
            return new(ActivityState.Malformed, Localizer["Invalid activity definition"], Localizer["Expected an activity object."], rawDefinition);

        if (StateMachineDesignerConstants.IsInvalidJsonSlotMarker(obj))
            return new(ActivityState.Malformed, Localizer["Invalid activity definition"], Localizer["The original source is preserved."], rawDefinition);

        var typeName = GetString(obj, "type");
        if (string.IsNullOrWhiteSpace(typeName))
            return new(ActivityState.Malformed, Localizer["Unavailable activity definition"], Localizer["The activity type is missing."], rawDefinition);

        var title = FirstNonEmpty(GetString(obj, "displayName"), GetString(obj, "name"), typeName)!;
        var version = GetString(obj, "version");
        var detail = string.IsNullOrWhiteSpace(version) ? typeName : $"{typeName} · v{version}";
        return new(ActivityState.Configured, Localizer[title], detail, rawDefinition);
    }

    private ConditionPresentation DescribeCondition(JsonNode? value)
    {
        var description = BooleanConditionAdapter.Inspect(value);
        return description.Kind switch
        {
            BooleanConditionKind.Missing => new(
                ConditionState.Missing,
                Localizer["Always"],
                Localizer["No condition configured; this transition evaluates as true."],
                string.Empty,
                string.Empty),
            BooleanConditionKind.Literal => BooleanCondition(
                description.LiteralValue == true,
                description.LiteralValue == true ? Localizer["Explicitly enabled."] : Localizer["Explicitly disabled."]),
            BooleanConditionKind.Expression => new(
                ConditionState.Expression,
                Localizer[description.ExpressionType ?? "Expression"],
                description.ExpressionValue ?? string.Empty,
                string.Empty,
                string.Empty),
            BooleanConditionKind.Unknown => new(
                ConditionState.Unknown,
                string.IsNullOrWhiteSpace(description.ExpressionType) ? Localizer["Custom condition"] : Localizer["Unavailable condition"],
                string.IsNullOrWhiteSpace(description.ExpressionType)
                    ? Localizer["A custom definition is preserved."]
                    : $"{description.ExpressionType} · {Localizer["definition preserved"]}",
                StateMachinePresentationFormatter.JsonSlot(value),
                string.Empty),
            _ => new(
                ConditionState.Malformed,
                Localizer["Invalid condition definition"],
                Localizer["The original source is preserved."],
                StateMachinePresentationFormatter.JsonSlot(value),
                Localizer["This condition cannot be evaluated until it is repaired or replaced."])
        };
    }

    private ConditionPresentation BooleanCondition(bool value, string detail) => value
        ? new(ConditionState.Always, Localizer["Always"], detail, string.Empty, string.Empty)
        : new(ConditionState.Never, Localizer["Never"], detail, string.Empty, Localizer["This transition cannot pass while the condition is false."]);

    private static string? GetString(JsonObject obj, string propertyName) => obj[propertyName] is JsonValue value && value.TryGetValue<string>(out var text)
        ? text
        : null;

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private enum ActivityState
    {
        Empty,
        Configured,
        Malformed
    }

    private enum ConditionState
    {
        Missing,
        Always,
        Never,
        Expression,
        Unknown,
        Malformed
    }

    private sealed record ActivityPresentation(ActivityState State, string Title, string Detail, string RawDefinition);

    private sealed record ConditionPresentation(ConditionState State, string Title, string Detail, string RawDefinition, string Warning);
}
