using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.List.Models;

internal record WorkflowInstanceRow(
    string WorkflowInstanceId,
    string? CorrelationId,
    WorkflowDefinitionSummary WorkflowDefinition,
    int Version,
    string? Name,
    WorkflowStatus Status,
    WorkflowSubStatus SubStatus,
    int IncidentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? FinishedAt);