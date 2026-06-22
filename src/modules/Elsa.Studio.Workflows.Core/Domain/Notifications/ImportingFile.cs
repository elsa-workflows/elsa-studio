using Elsa.Studio.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when an importing is file.
/// </summary>
[UsedImplicitly]
public record ImportingFile(IBrowserFile File) : INotification;
