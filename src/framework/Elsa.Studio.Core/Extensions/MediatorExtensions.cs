using Elsa.Studio.Contracts;

namespace Elsa.Studio.Extensions;

public static class MediatorExtensions
{
    public static void Subscribe<TNotification>(this IMediator mediator, INotificationHandler handler) where TNotification : INotification
    {
        var handlerType = handler.GetType();
        var subscribeMethod = typeof(IMediator).GetMethod(nameof(IMediator.Subscribe))!;
        var genericSubscribeMethod = subscribeMethod.MakeGenericMethod(typeof(TNotification), handlerType);
        genericSubscribeMethod.Invoke(mediator, new object[] { handler });
    }
    
    public static void Unsubscribe<TNotification>(this IMediator mediator, INotificationHandler handler) where TNotification : INotification
    {
        var handlerType = handler.GetType();
        var unsubscribeMethod = typeof(IMediator).GetMethod(nameof(IMediator.Unsubscribe))!;
        var genericUnsubscribeMethod = unsubscribeMethod.MakeGenericMethod(typeof(TNotification), handlerType);
        genericUnsubscribeMethod.Invoke(mediator, new object[] { handler });
    }
}