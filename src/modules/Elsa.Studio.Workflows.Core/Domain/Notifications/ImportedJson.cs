using Elsa.Studio.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Workflows.Domain.Notifications;

[UsedImplicitly]
public record ImportedJson(string Json) : INotification;