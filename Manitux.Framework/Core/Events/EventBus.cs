namespace CodeLogic.Core.Events;

/// <summary>
/// Thread-safe in-process event bus.
/// One instance is shared across all library, application, and plugin contexts.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<SubscriptionEntry>> _subscriptions = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Publish<T>(T @event) where T : IEvent
    {
        var entries = GetSnapshot(typeof(T));
        foreach (var entry in entries)
        {
            try
            {
                if (entry.SyncHandler != null)
                    entry.SyncHandler(@event);
                else if (entry.AsyncHandler != null)
                    // Fire-and-forget async handlers
                    _ = InvokeSafeAsync(entry.AsyncHandler, @event);
            }
            catch (Exception ex)
            {
                // Log to stderr — never crash on event handler errors
                Console.Error.WriteLine($"[EventBus] Handler error for {typeof(T).Name}: {ex.Message}");
            }
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T @event) where T : IEvent
    {
        var entries = GetSnapshot(typeof(T));
        foreach (var entry in entries)
        {
            try
            {
                if (entry.SyncHandler != null)
                    entry.SyncHandler(@event);
                else if (entry.AsyncHandler != null)
                    await entry.AsyncHandler(@event);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EventBus] Handler error for {typeof(T).Name}: {ex.Message}");
            }
        }
    }

    /// <inheritdoc />
    public IEventSubscription Subscribe<T>(Action<T> handler) where T : IEvent
    {
        var entry = new SubscriptionEntry
        {
            EventType = typeof(T),
            SyncHandler = e => handler((T)e)
        };
        AddEntry(typeof(T), entry);
        return new EventSubscription(this, typeof(T), entry);
    }

    /// <inheritdoc />
    public IEventSubscription SubscribeAsync<T>(Func<T, Task> handler) where T : IEvent
    {
        var entry = new SubscriptionEntry
        {
            EventType = typeof(T),
            AsyncHandler = e => handler((T)e)
        };
        AddEntry(typeof(T), entry);
        return new EventSubscription(this, typeof(T), entry);
    }

    internal void Unsubscribe(Type eventType, SubscriptionEntry entry)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var list))
                list.Remove(entry);
        }
    }

    private void AddEntry(Type eventType, SubscriptionEntry entry)
    {
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var list))
            {
                list = new List<SubscriptionEntry>();
                _subscriptions[eventType] = list;
            }
            list.Add(entry);
        }
    }

    private List<SubscriptionEntry> GetSnapshot(Type eventType)
    {
        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var list))
                return list.ToList(); // snapshot — don't hold lock during invocation
            return [];
        }
    }

    private static async Task InvokeSafeAsync(Func<object, Task> handler, object @event)
    {
        try { await handler(@event); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[EventBus] Async handler error: {ex.Message}");
        }
    }
}

internal sealed class SubscriptionEntry
{
    public required Type EventType { get; init; }
    public Action<object>? SyncHandler { get; init; }
    public Func<object, Task>? AsyncHandler { get; init; }
}

internal sealed class EventSubscription : IEventSubscription
{
    private readonly EventBus _bus;
    private readonly Type _eventType;
    private readonly SubscriptionEntry _entry;
    private bool _disposed;

    public EventSubscription(EventBus bus, Type eventType, SubscriptionEntry entry)
    {
        _bus = bus;
        _eventType = eventType;
        _entry = entry;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _bus.Unsubscribe(_eventType, _entry);
    }
}
