namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The identities Studio shares with elsa-core's <c>Elsa.Bpmn</c> module: the activity type name of a BPMN process
/// scope, and the outcomes such a scope completes with. One class so the designer, the port provider and the node's
/// display settings all name the same strings.
/// </summary>
public static class BpmnProcessConstants
{
    /// <summary>
    /// The activity type name of a BPMN process scope: elsa-core's <c>Elsa.Bpmn.Activities.BpmnProcess</c>.
    /// </summary>
    public const string ActivityTypeName = "Elsa.BpmnProcess";

    /// <summary>
    /// The outcome a BPMN process completes with normally. Mirrors <c>Bpmn.Semantics.BpmnInterpreter.DoneOutcomeName</c>,
    /// which Studio cannot reference: the interpreter runs server-side and Studio never takes a dependency on it.
    /// </summary>
    public const string DoneOutcomeName = "Done";

    /// <summary>
    /// The outcome a BPMN process completes with when a cancel end event cancelled a transaction. Mirrors
    /// <c>Bpmn.Semantics.BpmnInterpreter.CancelledOutcomeName</c>.
    /// </summary>
    public const string CancelledOutcomeName = "Cancelled";

    /// <summary>
    /// Every outcome a BPMN process scope can complete with, and therefore every outcome port a
    /// <c>Elsa.BpmnProcess</c> node offers on a flowchart.
    /// </summary>
    /// <remarks>
    /// The interpreter's <c>Complete</c> continuation carries exactly one of these two — see the documentation of
    /// <c>Bpmn.Semantics.BpmnContinuation.Complete.Outcome</c> — and elsa-core's <c>BpmnScopeHost</c> completes the
    /// activity with that one name. A document's end events therefore contribute no further outcome names: a none,
    /// terminate, message, escalation or compensate end event all complete the scope as <see cref="DoneOutcomeName"/>,
    /// and only a cancel end event on a transaction produces <see cref="CancelledOutcomeName"/>. Nothing in the
    /// <c>process</c> payload names an outcome of the scope itself either — a sequence flow's
    /// <c>conditionOutcome</c> names an outcome of the <em>work</em> the flow's source started, which is matched
    /// inside the scope and never surfaces on the enclosing flowchart.
    /// </remarks>
    public static IReadOnlyList<string> OutcomeNames { get; } = [DoneOutcomeName, CancelledOutcomeName];
}
