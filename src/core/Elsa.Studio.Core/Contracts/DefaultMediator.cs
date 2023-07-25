namespace Elsa.Studio.Contracts;

public class DefaultMediator : IMediator
{
    private readonly ISet<INotificationHandler> _handlers = new HashSet<INotificationHandler>();

    public void Subscribe<TNotification, THandler>(THandler handler) where TNotification : INotification where THandler : INotificationHandler<TNotification>
    {
        _handlers.Add(handler);
    }

    public void Unsubscribe<TNotification, THandler>(THandler handler) where TNotification : INotification where THandler : INotificationHandler<TNotification>
    {
        _handlers.Remove(handler);
    }

    public void Unsubscribe(INotificationHandler handler)
    {
        var handlers =_handlers.Where(x => x == handler).ToList();
        
        foreach (var h in handlers)
            _handlers.Remove(h);
    }

    public Task NotifyAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        var handlers = _handlers.OfType<INotificationHandler<TNotification>>().ToList();
        var tasks = handlers.Select(x => x.HandleAsync(notification, cancellationToken));
        return Task.WhenAll(tasks);
    }
}