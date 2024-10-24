using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

public record WorkflowDefinitionIdExported(string WorkflowDefinitionId, VersionOptions? VersionOptions, FileDownload FileDownload) : INotification;