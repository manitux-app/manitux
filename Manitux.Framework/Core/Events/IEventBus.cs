namespace CodeLogic.Core.Events;

/// <summary>Provides publish-subscribe event messaging within the application.</summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// Sync subscribers are called immediately on the calling thread.
    /// Async subscribers are fired-and-forgotten (not awaited).
    /// Use PublishAsync to await async subscribers.
    /// </summary>
    void Publish<T>(T @event) where T : IEvent;

    /// <summary>
    /// Publishes an event and awaits all subscribers (sync and async).
    /// </summary>
    Task PublishAsync<T>(T @event) where T : IEvent;

    /// <summary>
    /// Subscribes a synchronous handler for events of type T.
    /// Returns a subscription that can be disposed to unsubscribe.
    /// </summary>
    IEventSubscription Subscribe<T>(Action<T> handler) where T : IEvent;

    /// <summary>
    /// Subscribes an asynchronous handler for events of type T.
    /// Returns a subscription that can be disposed to unsubscribe.
    /// </summary>
    IEventSubscription SubscribeAsync<T>(Func<T, Task> handler) where T : IEvent;
}
