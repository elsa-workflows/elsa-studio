using Elsa.Dashboard.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Dashboard.Services;

public class DefaultAppBarService : IAppBarService
{
    private readonly ICollection<RenderFragment> _appBarItems = new List<RenderFragment>();
    private int _appBarSequence;
    
    public event Action? AppBarItemsChanged;
    public IEnumerable<RenderFragment> AppBarItems => _appBarItems.ToList();
    
    public void AddAppBarItem<T>() where T : IComponent
    {
        _appBarItems.Add(builder =>
        {
            builder.OpenComponent<T>(_appBarSequence++);
            builder.CloseComponent();
        });
        
        AppBarItemsChanged?.Invoke();
    }
}