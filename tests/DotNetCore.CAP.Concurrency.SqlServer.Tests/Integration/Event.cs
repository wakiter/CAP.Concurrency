using System;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}