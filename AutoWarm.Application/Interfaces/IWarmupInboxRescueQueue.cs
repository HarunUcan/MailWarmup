using System;
using System.Collections.Generic;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupInboxRescueQueue
{
    void Enqueue(Guid mailAccountId);
    IReadOnlyCollection<Guid> DequeueAll();
}
