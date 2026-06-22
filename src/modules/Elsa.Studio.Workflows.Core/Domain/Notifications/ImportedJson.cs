using Elsa.Studio.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when an imported is json.
/// </summary>
[UsedImplicitly]
public record ImportedJson(string Json) : INotification;
