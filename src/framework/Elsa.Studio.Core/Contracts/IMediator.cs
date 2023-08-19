namespace Elsa.Studio.Contracts;

public interface IMediator
{
    void Subscribe<TNotification, THandler>(THandler handler) where THandler : INotificationHandler<TNotification> where TNotification : INotification;
    void Unsubscribe<TNotification, THandler>(THandler handler) where THandler : INotificationHandler<TNotification> where TNotification : INotification;
    void Unsubscribe(INotificationHandler handler);
    Task NotifyAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
}