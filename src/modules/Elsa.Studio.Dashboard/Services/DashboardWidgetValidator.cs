using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Dashboard.Services;

public static class DashboardWidgetValidator
{
    public static DashboardWidgetDescriptor Validate(DashboardWidgetDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Id))
            throw new InvalidOperationException("Dashboard widget descriptors require a stable non-empty ID.");

        if (!Enum.IsDefined(descriptor.Zone))
            throw new InvalidOperationException($"Dashboard widget descriptor '{descriptor.Id}' uses unsupported zone '{descriptor.Zone}'.");

        if (!Enum.IsDefined(descriptor.Span))
            throw new InvalidOperationException($"Dashboard widget descriptor '{descriptor.Id}' uses unsupported span '{descriptor.Span}'.");

        return descriptor;
    }
}
