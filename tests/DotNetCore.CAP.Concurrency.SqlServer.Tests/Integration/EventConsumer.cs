using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration;

public class EventConsumer : ICapSubscribe
{
    public readonly Dictionary<Guid, int> ReceivedEvents = new();
    private readonly object _synchronisationObject = new object();

    [CapSubscribe("Consumer_Event")]
    public Task Consume(Event ev)
    {
        int invocationCount;
        lock (_synchronisationObject)
        {
            ReceivedEvents.TryGetValue(ev.Id, out invocationCount);
            ReceivedEvents[ev.Id] = ++invocationCount;
        }

        if (invocationCount <= 5)
        {
            throw null;
        }

        return Task.CompletedTask;

        //if (invocationCount == 4)
        //{
        //    return Task.CompletedTask;
        //}

        //throw null;
    }
}