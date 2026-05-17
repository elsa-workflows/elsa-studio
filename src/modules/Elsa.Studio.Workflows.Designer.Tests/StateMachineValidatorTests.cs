using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Designer.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public class StateMachineValidatorTests
{
    private readonly StateMachineValidator _validator = new();

    [Fact]
    public void Validate_ReportsBlockingStateAndTransitionReferenceErrors()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode(),
                new StateMachineStateNode { Name = "Paid" },
                new StateMachineStateNode { Name = "Paid" }
            },
            Transitions =
            {
                new StateMachineTransitionEdge { Name = "MissingSource", To = "Paid" },
                new StateMachineTransitionEdge { Name = "MissingTarget", From = "NewOrder" }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "EmptyStateName" && x.Severity == StateMachineValidationSeverity.Error);
        Assert.Contains(issues, x => x.Code == "DuplicateStateName" && x.Target == "Paid");
        Assert.Contains(issues, x => x.Code == "MissingTransitionSource");
        Assert.Contains(issues, x => x.Code == "MissingTransitionSourceState" && x.Target == "MissingTarget");
        Assert.Contains(issues, x => x.Code == "AmbiguousTransitionTargetState" && x.Target == "MissingSource");
    }

    [Fact]
    public void Validate_ReportsMissingInitialAndCurrentStateWarnings()
    {
        var graph = new StateMachineGraph
        {
            InitialState = "NewOrder",
            CurrentState = "Closed",
            States = { new StateMachineStateNode { Name = "Paid" } }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "MissingInitialState" && x.Severity == StateMachineValidationSeverity.Warning);
        Assert.Contains(issues, x => x.Code == "MissingCurrentState" && x.Severity == StateMachineValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_ReportsTransitionsBrokenByStateRename()
    {
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "PaymentReceived" },
                new StateMachineStateNode { Name = "Closed" }
            },
            Transitions =
            {
                new StateMachineTransitionEdge { Name = "Close", From = "Paid", To = "Closed" }
            }
        };

        var issues = _validator.Validate(graph);

        Assert.Contains(issues, x => x.Code == "MissingTransitionSourceState" && x.Target == "Close");
    }
}
