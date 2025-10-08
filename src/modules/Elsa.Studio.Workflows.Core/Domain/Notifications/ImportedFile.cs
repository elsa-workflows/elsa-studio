using Elsa.Studio.Contracts;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Notifications;

#if JETBRAINS_ANNOTATIONS
[UsedImplicitly]
#endif
public record ImportedFile(IBrowserFile File) : INotification;