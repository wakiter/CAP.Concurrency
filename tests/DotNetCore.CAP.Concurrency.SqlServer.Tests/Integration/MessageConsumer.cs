using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration;

public class MessageConsumer : ICapSubscribe
{
    public const string Group = "Consumer_Message";
    public readonly Dictionary<Guid, int> ReceivedEvents = new();
    private readonly object _synchronisationObject = new object();

    [CapSubscribe(Group)]
    public Task Consume(Message ev)
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
    }
}