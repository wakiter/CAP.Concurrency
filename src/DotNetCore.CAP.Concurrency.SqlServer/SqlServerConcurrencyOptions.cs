namespace DotNetCore.CAP.Concurrency.SqlServer
{
    public class SqlServerConcurrencyOptions : ConcurrencyOptions
    {
        public string Schema { get; set; } = "cap";

        public override string MessagesReceivedInFlightStorageName => "MessagesReceivedInFlight";
    }
}