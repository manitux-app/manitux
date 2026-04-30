namespace CodeLogic.Core.Events;

/// <summary>
/// Represents an active event subscription.
/// Dispose to unsubscribe and stop receiving events.
/// </summary>
public interface IEventSubscription : IDisposable { }
