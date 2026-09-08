namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// What a click, double-click, or selection on a BPMN element tells .NET. Mirrors the TypeScript
/// <c>BpmnElementSelection</c> that <c>HandleActivitySelected</c> and <c>HandleActivityDoubleClick</c>
/// receive from the BPMN canvas.
/// </summary>
/// <param name="ElementId">The BPMN element id. Unique in the document.</param>
/// <param name="ElementType">The raw BPMN element type, e.g. <c>serviceTask</c>, <c>boundaryEvent</c>.</param>
/// <param name="Kind">The element family: <c>event</c>, <c>gateway</c>, <c>task</c>, <c>callActivity</c>, <c>subProcess</c>, <c>unknown</c>.</param>
/// <param name="Name">The element's document name, if any.</param>
/// <param name="ActivityId">The Elsa activity bound to this element, or null when nothing is or should be.</param>
/// <param name="BindingState">How the binding resolves, or null for an element that performs no work.</param>
/// <param name="ScopeId">The process id of the scope the element lives in.</param>
/// <param name="ScopeActivityId">The <c>Elsa.BpmnProcess</c> activity that runs that scope.</param>
/// <param name="BoundaryHostElementId">For a boundary event, the element it is attached to.</param>
/// <param name="ChildScopeId">For a subprocess, the process id of the scope it contains.</param>
public record BpmnElementSelection(
    string ElementId,
    string ElementType,
    string Kind,
    string? Name,
    string? ActivityId,
    string? BindingState,
    string ScopeId,
    string ScopeActivityId,
    string? BoundaryHostElementId,
    string? ChildScopeId);
