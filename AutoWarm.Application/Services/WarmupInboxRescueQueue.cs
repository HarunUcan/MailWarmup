using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AutoWarm.Application.Interfaces;

namespace AutoWarm.Application.Services;

/// <summary>
/// Thread-safe queue to trigger immediate inbox rescues for specific mail accounts.
/// </summary>
public class WarmupInboxRescueQueue : IWarmupInboxRescueQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();
    private readonly ConcurrentDictionary<Guid, byte> _enqueued = new();

    public void Enqueue(Guid mailAccountId)
    {
        if (_enqueued.TryAdd(mailAccountId, 0))
        {
            _queue.Enqueue(mailAccountId);
        }
    }

    public IReadOnlyCollection<Guid> DequeueAll()
    {
        var items = new List<Guid>();
        while (_queue.TryDequeue(out var id))
        {
            items.Add(id);
            _enqueued.TryRemove(id, out _);
        }

        return items;
    }
}
