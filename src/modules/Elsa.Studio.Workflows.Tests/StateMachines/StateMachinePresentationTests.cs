using System.Text.Json.Nodes;
using Bunit;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
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
    public void TransitionInspector_EmitsRouteAndSlotChangesWithExplicitlyLabelledControls()
    {
        var transition = new StateMachineTransitionEdge
        {
            Name = "Approve",
            From = "Pending",
            To = "Approved"
        };
        var states = new[]
        {
            new StateMachineStateNode { Name = "Pending" },
            new StateMachineStateNode { Name = "Approved" }
        };
        string? changedFrom = null;
        string? changedName = "not-called";
        StateMachineSlotValueChange? changedSlot = null;
        var cut = Render<StateMachineTransitionInspector>(parameters => parameters
            .Add(component => component.Transition, transition)
            .Add(component => component.States, states)
            .Add(component => component.FromChanged, value => changedFrom = value)
            .Add(component => component.NameChanged, value => changedName = value)
            .Add(component => component.SlotChanged, value => changedSlot = value));

        var fromSelect = cut.Find("select[id$='-from']");
        Assert.Equal("From", cut.Find($"label[for='{fromSelect.Id}']").TextContent);

        fromSelect.Change("Approved");
        cut.Find("input[id$='-name']").Change("");
        cut.Find("textarea[id$='-condition']").Change("{\"type\":\"Boolean\"}");

        Assert.Equal("Approved", changedFrom);
        Assert.Null(changedName);
        Assert.Equal("condition", changedSlot?.SlotName);
        Assert.Equal("{\"type\":\"Boolean\"}", changedSlot?.Value);
        Assert.Equal("Pending", transition.From);
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);

        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
