using System.Collections.Concurrent;
using System.Threading.Channels;

namespace besti2.Infrastructure.Persistence;

// "Something changed in this bell": the open /api/notifications/stream connections wait here and send the new
// number at once. In memory, so it works with one API server (enough for now; several servers would need Redis)
public static class NotificationSignals
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<bool>>> Listeners = new();

    // businessId set = the business's bell, otherwise the customer's
    public static string Key(string? userId, Guid? businessId) => businessId is Guid id ? $"b:{id}" : $"u:{userId}";

    // One open connection; dispose it when the browser goes away
    public static Subscription Subscribe(string key)
    {
        var channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite // one waiting signal is enough
        });
        var id = Guid.NewGuid();
        Listeners.GetOrAdd(key, _ => new ConcurrentDictionary<Guid, Channel<bool>>())[id] = channel;
        return new Subscription(channel.Reader, () =>
        {
            if (Listeners.TryGetValue(key, out var forKey))
            {
                forKey.TryRemove(id, out _);
            }
        });
    }

    public static void Changed(string key)
    {
        if (Listeners.TryGetValue(key, out var forKey))
        {
            foreach (var channel in forKey.Values)
            {
                channel.Writer.TryWrite(true);
            }
        }
    }

    // A new notification was saved: wakes the push job (see PushSenderJob). One waiting signal is enough
    private static readonly Channel<bool> NewOnes = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public static void Added() => NewOnes.Writer.TryWrite(true);

    // Waits for a new notification, at most maxWait
    public static async Task WaitForNewAsync(TimeSpan maxWait, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(maxWait);
        try
        {
            await NewOnes.Reader.ReadAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // maxWait has passed: look anyway
        }
    }

    public sealed class Subscription(ChannelReader<bool> reader, Action unsubscribe) : IDisposable
    {
        public ChannelReader<bool> Reader { get; } = reader;
        public void Dispose() => unsubscribe();
    }
}