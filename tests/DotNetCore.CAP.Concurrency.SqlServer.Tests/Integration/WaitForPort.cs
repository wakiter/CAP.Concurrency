namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration;

public class WaitForPort : WaitFor
{
    public WaitForPort(string service, int port, string protocol)
    {
        Service = service;
        Port = port;
        Protocol = protocol;
    }

    public string Service { get; }

    public int Port { get; }

    public string Protocol { get; }
}