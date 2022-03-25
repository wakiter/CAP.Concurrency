using System;

namespace DotNetCore.CAP.Concurrency
{
    public abstract class ConcurrencyOptions
    {
        public TimeSpan InFlightTime { get; set; } = TimeSpan.FromSeconds(30);

        public abstract string? MessagesReceivedInFlightStorageName { get; }
    }
}