using Elsa.Studio.Contracts;
#if JETBRAINS_ANNOTATIONS
using JetBrains.Annotations;
#endif

namespace Elsa.Studio.Workflows.Domain.Notifications;

#if JETBRAINS_ANNOTATIONS
[UsedImplicitly]
#endif
public record ImportedJson(string Json) : INotification;