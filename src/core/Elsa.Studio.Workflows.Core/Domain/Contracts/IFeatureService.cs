namespace Elsa.Studio.Workflows.Domain.Contracts;

public interface IFeatureService
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);
}