namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration;

public class WaitForHttp : WaitFor
{
    public WaitForHttp(string service, string url)
    {
        Service = service;
        Url = url;
    }

    public string Service { get; }

    public string Url { get; }
}