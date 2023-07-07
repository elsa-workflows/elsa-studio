using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.List.Models;

public record WorkflowInstanceRow(
    string WorkflowInstanceId,
    string? CorrelationId,
    WorkflowDefinitionSummary WorkflowDefinition,
    int Version,
    string? Name,
    WorkflowStatus Status,
    WorkflowSubStatus SubStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastExecutedAt,
    DateTimeOffset? FinishedAt,
    DateTimeOffset? FaultedAt);