using System;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}