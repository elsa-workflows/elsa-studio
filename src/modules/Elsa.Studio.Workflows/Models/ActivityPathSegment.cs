namespace Elsa.Studio.Workflows.Models;

/// <summary>
/// Represents a path segment in a workflow.
/// </summary>
/// <param name="ActivityId">The ID of the activity.</param>
/// <param name="PortName">The name of the port.</param>
public record ActivityPathSegment(string ActivityId, string PortName);