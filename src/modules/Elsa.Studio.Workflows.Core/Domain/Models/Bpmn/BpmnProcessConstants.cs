namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The identities Studio shares with elsa-core's <c>Elsa.Bpmn</c> module: the activity type name of a BPMN process
/// scope, and the outcome such a scope completes with normally. One class so the designer and the node's display
/// settings all name the same strings.
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
}
