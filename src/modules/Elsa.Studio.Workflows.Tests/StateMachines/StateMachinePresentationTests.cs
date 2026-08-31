using System.Text.Json.Nodes;
using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.StateMachines;

public sealed class StateMachinePresentationTests : BunitContext, IAsyncLifetime
{
    public StateMachinePresentationTests()
    {
        Services.AddSingleton<ILocalizer, TestLocalizer>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void Outline_ExposesSelectionAndStateStatusWithoutRelyingOnColor()
    {
        var pending = new StateMachineStateNode { Name = "Pending" };
        var approved = new StateMachineStateNode { Name = "Approved", IsTerminal = true };
        var graph = new StateMachineGraph
        {
            InitialState = pending.Name,
            CurrentState = pending.Name,
            States = { pending, approved },
            Transitions =
            {
                new StateMachineTransitionEdge
                {
                    Name = "Approve",
                    From = pending.Name,
                    To = approved.Name,
                    Trigger = new JsonObject { ["type"] = "Button" }
                }
            }
        };

        var cut = Render<StateMachineOutline>(parameters => parameters
            .Add(component => component.Graph, graph)
            .Add(component => component.SelectedStateName, pending.Name));

        var selectedState = cut.Find("[data-state-name='Pending']");
        Assert.Equal("true", selectedState.GetAttribute("aria-pressed"));
        Assert.Contains("Initial", selectedState.TextContent);
        Assert.Contains("Current", selectedState.TextContent);
        Assert.Contains("1 outgoing transition", selectedState.TextContent);

        var terminalState = cut.Find("[data-state-name='Approved']");
        Assert.Equal("false", terminalState.GetAttribute("aria-pressed"));
        Assert.Contains("Terminal", terminalState.TextContent);

        var transition = cut.Find("[data-transition-index='0']");
        Assert.Contains("Pending", transition.TextContent);
        Assert.Contains("Approved", transition.TextContent);
        Assert.Contains("Trigger", transition.TextContent);
    }

    [Fact]
    public void Outline_EmitsTheSelectedGraphItem()
    {
        var state = new StateMachineStateNode { Name = "Pending" };
        var transition = new StateMachineTransitionEdge { From = "Pending", To = "Approved" };
        var graph = new StateMachineGraph { States = { state }, Transitions = { transition } };
        StateMachineStateNode? selectedState = null;
        StateMachineTransitionEdge? selectedTransition = null;
        var cut = Render<StateMachineOutline>(parameters => parameters
            .Add(component => component.Graph, graph)
            .Add(component => component.StateSelected, value => selectedState = value)
            .Add(component => component.TransitionSelected, value => selectedTransition = value));

        cut.Find("[data-state-name='Pending']").Click();
        cut.Find("[data-transition-index='0']").Click();

        Assert.Same(state, selectedState);
        Assert.Same(transition, selectedTransition);
    }

    [Fact]
    public void StateInspector_AssociatesLabelsAndEmitsEditsWithoutMutatingTheModel()
    {
        var state = new StateMachineStateNode
        {
            Name = "Pending",
            Entry = new JsonObject { ["type"] = "WriteLine" }
        };
        string? changedName = null;
        StateMachineSlotValueChange? changedSlot = null;
        string? clearedSlot = null;
        var cut = Render<StateMachineStateInspector>(parameters => parameters
            .Add(component => component.State, state)
            .Add(component => component.NameChanged, value => changedName = value)
            .Add(component => component.SlotChanged, value => changedSlot = value)
            .Add(component => component.SlotCleared, value => clearedSlot = value));

        var nameInput = cut.Find("input[id$='-name']");
        var nameLabel = cut.Find($"label[for='{nameInput.Id}']");
        Assert.Equal("Name", nameLabel.TextContent);

        nameInput.Change("Review");
        cut.Find("textarea[id$='-entry']").Change("{\"type\":\"RunTask\"}");
        cut.Find("textarea[id$='-entry']").ParentElement!.QuerySelector("button")!.Click();

        Assert.Equal("Review", changedName);
        Assert.Equal("Pending", state.Name);
        Assert.Equal("entry", changedSlot?.SlotName);
        Assert.Equal("{\"type\":\"RunTask\"}", changedSlot?.Value);
        Assert.Equal("entry", clearedSlot);
    }

    [Fact]
    public void StateInspector_ReadOnlyModeSuppressesDestructiveActionsAndDisablesFields()
    {
        var cut = Render<StateMachineStateInspector>(parameters => parameters
            .Add(component => component.State, new StateMachineStateNode { Name = "Pending" })
            .Add(component => component.IsReadOnly, true));

        Assert.True(cut.Find("input[id$='-name']").HasAttribute("disabled"));
        Assert.True(cut.Find("textarea[id$='-entry']").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void TransitionInspector_RendersExecutionStoryAndSemanticSlotSummaries()
    {
        var transition = new StateMachineTransitionEdge
        {
            Name = "Approve",
            From = "Pending",
            To = "Approved",
            Trigger = new JsonObject { ["type"] = "Elsa.Event" },
            Condition = JsonValue.Create(true),
            Action = new JsonObject { ["type"] = "Elsa.WriteLine", ["text"] = "Approved" }
        };
        var states = new[]
        {
            new StateMachineStateNode { Name = "Pending" },
            new StateMachineStateNode { Name = "Approved" }
        };
        string? editedCondition = null;
        string? changedFrom = null;
        string? changedName = "not-called";
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, transition)
            .Add(component => component.States, states)
            .Add(component => component.ConditionEditRequested, value => editedCondition = value)
            .Add(component => component.FromChanged, value => changedFrom = value)
            .Add(component => component.NameChanged, value => changedName = value));

        var markup = cut.Markup;
        Assert.True(markup.IndexOf("WHEN", StringComparison.Ordinal) < markup.IndexOf("ONLY IF", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("ONLY IF", StringComparison.Ordinal) < markup.IndexOf("THEN", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("THEN", StringComparison.Ordinal) < markup.IndexOf("TO", StringComparison.Ordinal));
        Assert.Equal(4, cut.FindAll("[data-transition-slot]").Count);
        Assert.Contains("Elsa.Event", cut.Find("[data-transition-slot='trigger']").TextContent);
        Assert.Contains("Elsa.WriteLine", cut.Find("[data-transition-slot='action']").TextContent);
        Assert.Equal("always", cut.Find("[data-transition-slot='condition'] [data-condition-state]").GetAttribute("data-condition-state"));
        Assert.NotEmpty(cut.FindAll("[data-slot-action='open']"));
        Assert.NotEmpty(cut.FindAll("[data-slot-action='replace']"));
        Assert.NotEmpty(cut.FindAll("[data-slot-action='clear']"));

        cut.Find("select[id$='-from']").Change("Approved");
        cut.Find("input[id$='-name']").Change("");
        cut.Find("[data-transition-slot='condition'] [data-slot-action='edit']").Click();

        Assert.Equal("Approved", changedFrom);
        Assert.Null(changedName);
        Assert.Equal("condition", editedCondition);
        Assert.True(transition.Condition!.GetValue<bool>());
        Assert.Equal("Pending", transition.From);
        Assert.DoesNotContain("textarea", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TransitionInspector_DescribesMissingAndFalseConditionsWithoutMutatingThem()
    {
        var missing = new StateMachineTransitionEdge { From = "Pending", To = "Approved" };
        var missingCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition, missing));

        Assert.Contains("No trigger configured", missingCut.Find("[data-transition-slot='trigger']").TextContent);
        Assert.Contains("evaluates immediately", missingCut.Find("[data-transition-slot='trigger']").TextContent);
        Assert.Equal("missing", missingCut.Find("[data-transition-slot='condition'] [data-condition-state]").GetAttribute("data-condition-state"));
        Assert.Contains("Always", missingCut.Find("[data-transition-slot='condition']").TextContent);
        Assert.Null(missing.Condition);

        var falseCondition = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Condition = JsonValue.Create(false)
        };
        var falseCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition, falseCondition));

        var condition = falseCut.Find("[data-transition-slot='condition']");
        Assert.Equal("never", condition.QuerySelector("[data-condition-state]")!.GetAttribute("data-condition-state"));
        Assert.Contains("Never", condition.TextContent);
        Assert.Contains("cannot pass", condition.TextContent);
        Assert.False(falseCondition.Condition!.GetValue<bool>());
    }

    [Fact]
    public void TransitionInspector_DescribesCanonicalWrappedBooleanConditionsWithoutNormalizingThem()
    {
        var trueCondition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "Literal", ["value"] = true }
        };
        var falseCondition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "Literal", ["value"] = false }
        };
        var trueSource = trueCondition.ToJsonString();
        var falseSource = falseCondition.ToJsonString();

        var trueCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = trueCondition }));
        var falseCut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = falseCondition }));

        Assert.Equal("always", trueCut.Find("[data-condition-state]").GetAttribute("data-condition-state"));
        Assert.Contains("Always", trueCut.Find("[data-testid='state-machine-transition-condition-summary']").TextContent);
        Assert.Equal("never", falseCut.Find("[data-condition-state]").GetAttribute("data-condition-state"));
        Assert.Contains("Never", falseCut.Find("[data-testid='state-machine-transition-condition-summary']").TextContent);
        Assert.Equal(trueSource, trueCondition.ToJsonString());
        Assert.Equal(falseSource, falseCondition.ToJsonString());
    }

    [Fact]
    public void TransitionInspector_DescribesCanonicalWrappedProviderExpression()
    {
        var condition = new JsonObject
        {
            ["typeName"] = "Boolean",
            ["expression"] = new JsonObject { ["type"] = "JavaScript", ["value"] = "context.input === true" }
        };
        var source = condition.ToJsonString();
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition,
            new StateMachineTransitionEdge { From = "Pending", To = "Approved", Condition = condition }));

        var summary = cut.Find("[data-testid='state-machine-transition-condition-summary']");
        Assert.Equal("expression", summary.GetAttribute("data-condition-state"));
        Assert.Contains("JavaScript", summary.TextContent);
        Assert.Contains("context.input === true", summary.TextContent);
        Assert.Equal(source, condition.ToJsonString());
    }

    [Fact]
    public void TransitionInspector_PreservesMalformedAndUnknownDefinitionsForInspection()
    {
        var transition = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Trigger = new JsonObject { ["id"] = "LegacyTrigger", ["payload"] = "keep-me" },
            Condition = new JsonObject { ["type"] = "Contoso.CustomCondition", ["payload"] = "keep-me" },
            Action = JsonValue.Create("not-an-activity")
        };

        var cut = Render<StateMachineTransitionInspector>(parameters => parameters.Add(component => component.Transition, transition));

        Assert.Equal("malformed", cut.Find("[data-transition-slot='trigger'] [data-activity-state]").GetAttribute("data-activity-state"));
        Assert.Contains("keep-me", cut.Find("[data-testid='state-machine-transition-trigger-definition']").TextContent);
        Assert.Equal("unknown", cut.Find("[data-transition-slot='condition'] [data-condition-state]").GetAttribute("data-condition-state"));
        Assert.Contains("Contoso.CustomCondition", cut.Find("[data-testid='state-machine-transition-condition-definition']").TextContent);
        Assert.Equal("malformed", cut.Find("[data-transition-slot='action'] [data-activity-state]").GetAttribute("data-activity-state"));
        Assert.Contains("not-an-activity", cut.Find("[data-testid='state-machine-transition-action-definition']").TextContent);
        Assert.NotEmpty(cut.FindAll("[data-transition-slot='trigger'] [data-slot-action='replace']"));
        Assert.Empty(cut.FindAll("[data-transition-slot='trigger'] [data-slot-action='open']"));
    }

    [Fact]
    public void TransitionInspector_EmitsSemanticActivityCallbacksWithSlotNames()
    {
        var slots = new List<string>();
        var configured = new StateMachineTransitionEdge
        {
            From = "Pending",
            To = "Approved",
            Trigger = new JsonObject { ["type"] = "Elsa.Event" },
            Action = new JsonObject { ["type"] = "Elsa.WriteLine" }
        };
        var configuredCut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, configured)
            .Add(component => component.ActivityOpenRequested, slot => slots.Add($"open:{slot}"))
            .Add(component => component.ActivityReplaceRequested, slot => slots.Add($"replace:{slot}"))
            .Add(component => component.ActivityClearRequested, slot => slots.Add($"clear:{slot}"))
            .Add(component => component.ActivityDropRequested, slot => slots.Add($"drop:{slot}")));

        configuredCut.Find("[data-transition-slot='trigger'] [data-slot-action='open']").Click();
        configuredCut.Find("[data-transition-slot='action'] [data-slot-action='replace']").Click();
        configuredCut.Find("[data-transition-slot='action'] [data-slot-action='clear']").Click();
        configuredCut.Find("[data-transition-slot='trigger'] [data-slot-action='drop']").TriggerEvent("ondrop", new DragEventArgs());

        var emptyCut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, new StateMachineTransitionEdge { From = "Pending", To = "Approved" })
            .Add(component => component.ActivityAddRequested, slot => slots.Add($"add:{slot}")));
        emptyCut.Find("[data-transition-slot='action'] [data-slot-action='add']").Click();

        Assert.Equal(["open:trigger", "replace:action", "clear:action", "drop:trigger", "add:action"], slots);
    }

    [Fact]
    public void TransitionInspector_ReadOnlyModeRetainsInspectionButSuppressesMutationControlsAndDrops()
    {
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, new StateMachineTransitionEdge
            {
                From = "Pending",
                To = "Approved",
                Trigger = new JsonObject { ["type"] = "Elsa.Event" },
                Condition = JsonValue.Create(false),
                Action = new JsonObject { ["type"] = "Elsa.WriteLine" }
            })
            .Add(component => component.IsReadOnly, true));

        Assert.Contains("WHEN", cut.Markup);
        Assert.Contains("Never", cut.Markup);
        Assert.Equal(2, cut.FindAll("[data-slot-action='open']").Count);
        Assert.Empty(cut.FindAll("[data-slot-action='add']"));
        Assert.Empty(cut.FindAll("[data-slot-action='replace']"));
        Assert.Empty(cut.FindAll("[data-slot-action='clear']"));
        Assert.Empty(cut.FindAll("[data-slot-action='edit']"));
        Assert.All(cut.FindAll("[data-slot-action='drop']"), element => Assert.False(element.HasAttribute("ondrop")));
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);

        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
