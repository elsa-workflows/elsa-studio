using DEDrake;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

public class ShortGuidActivityIdGenerator : IActivityIdGenerator
{
    public string GenerateId() => ShortGuid.NewGuid().ToString();
}