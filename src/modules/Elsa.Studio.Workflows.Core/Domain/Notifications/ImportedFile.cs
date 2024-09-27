using Elsa.Studio.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Notifications;

[UsedImplicitly]
public record ImportedFile(IBrowserFile File) : INotification;