using DEDrake;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// Generates unique activity IDs using <see cref="ShortGuid"/>.
/// </summary>
public class ShortGuidActivityIdGenerator : IActivityIdGenerator
{
    /// <inheritdoc />
    public string GenerateId() => ShortGuid.NewGuid().ToString();
}