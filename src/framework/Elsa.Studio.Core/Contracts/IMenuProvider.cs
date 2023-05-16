using Elsa.Studio.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides menu items to the dashboard.
/// </summary>
public interface IMenuProvider
{
    ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default);
}