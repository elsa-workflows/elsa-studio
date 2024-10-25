using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionIdExporting(string WorkflowDefinitionId, VersionOptions? VersionOptions) : INotification;