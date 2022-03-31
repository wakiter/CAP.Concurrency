using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration;

public sealed class ConcurrencyTests : IntegrationTestBase
{
    private const string DockerDependencies = "docker.dependencies.yml";

    private readonly ITestOutputHelper _outputHelper;

    public ConcurrencyTests(ITestOutputHelper outputHelper)
        : base(DockerDependencies, new WaitForPort("rabbitmq", 5672, "tcp"), new WaitForPort("sql", 1433, "tcp"))
    {
        _outputHelper = outputHelper;
    }

    [Theory]
    [InlineData(CapRunMode.Serialised, 60, 60, 5, 7)]
    [InlineData(CapRunMode.Chaos, 60, 60, 5, 7)]
    public async Task Multi_Instances_Are_Executed_In_Mode(
        CapRunMode mode,
        int publisherFailedRetryInterval,
        int publisherInFlightTime,
        int consumerFailedRetryInterval,
        int consumerInFlightTime)
    {
        await WaitForContainers();

        var masterConnectionString = GetConnectionString("master");
        var capConnectionString = GetConnectionString("CAP");

        CreateDatabaseIfNotExists(masterConnectionString, "CAP");

        var publisher = await GetCapPublisher(publisherFailedRetryInterval, publisherInFlightTime, capConnectionString);
        var consumerOne = await GetConsumer<MessageConsumer>(consumerFailedRetryInterval, consumerInFlightTime, capConnectionString, mode == CapRunMode.Serialised);
        var consumerTwo = await GetConsumer<MessageConsumer>(consumerFailedRetryInterval, consumerInFlightTime, capConnectionString, mode == CapRunMode.Serialised);

        var publishedEvent = new Message();

        await publisher.PublishAsync(MessageConsumer.Group, publishedEvent);

        await Task.Delay(TimeSpan.FromSeconds(25));

        var overallInvocationCount = consumerOne.ReceivedEvents.Sum(x => x.Value)
                                     + consumerTwo.ReceivedEvents.Sum(x => x.Value);

        if (mode == CapRunMode.Serialised)
        {
            overallInvocationCount.Should().Be(5);
        }
        else if (mode == CapRunMode.Chaos)
        {
            overallInvocationCount.Should().NotBe(5).And.BeGreaterThan(0); //it shows that this is barely predictable
        }
        else
        {
            throw new ArgumentException($"{mode} is not recognised!");
        }

        Task WaitForContainers()
        {
            return Task.Delay(TimeSpan.FromSeconds(10));
        }

        string GetConnectionString(string initialCatalog)
        {
            var sqlConnectionStringBuilder = GetDefaultConnectionStringBuilder();
            sqlConnectionStringBuilder.InitialCatalog = initialCatalog;

            return sqlConnectionStringBuilder.ConnectionString;
        }

        SqlConnectionStringBuilder GetDefaultConnectionStringBuilder()
        {
            var sqlContainer = DockerContainer.Containers.First(x => x.Name == "sql");
            var saPasswordEnvSettings = sqlContainer.GetConfiguration().Config.Env.FirstOrDefault(x => x.StartsWith("sa_password", StringComparison.InvariantCultureIgnoreCase));
            var saPassword = saPasswordEnvSettings!.Split('=').Last();

            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
            {
                UserID = "sa",
                Password = saPassword,
                DataSource = "localhost"
            };

            return sqlConnectionStringBuilder;
        }

        async Task<ICapPublisher> GetCapPublisher(int failedRetryInterval, int inFlightTimeInSeconds, string connectionString)
        {
            var publisherCollection = new ServiceCollection();
            publisherCollection.RegisterCapCommons(failedRetryInterval, inFlightTimeInSeconds, true, connectionString);

            var publisherSp = publisherCollection.BuildServiceProvider();

            publisherSp
                .GetRequiredService<IStoreDatabaseSettings>()
                .StoreConnectionString(connectionString);

            await publisherSp.GetRequiredService<IBootstrapper>().BootstrapAsync();

            var capPublisher = publisherSp.GetRequiredService<ICapPublisher>();

            return capPublisher;
        }

        async Task<TMessageConsumer> GetConsumer<TMessageConsumer>(int failedRetryInterval, int inFlightTimeInSeconds, string connectionString, bool multiInstanceConcurrency) where TMessageConsumer : class
        {
            var consumerCollection = new ServiceCollection();
            consumerCollection.RegisterCapCommons(failedRetryInterval, inFlightTimeInSeconds, multiInstanceConcurrency, connectionString);

            consumerCollection.AddSingleton<TMessageConsumer>();

            consumerCollection.Decorate<IDataStorage, SqlServerDataStorageWithFasterRetriesDecorator>();
            consumerCollection.AddSingleton(sp => (SqlServerDataStorageWithFasterRetriesDecorator) sp.GetRequiredService<IDataStorage>());

            consumerCollection.AddLogging(x => x.AddXUnit(_outputHelper));

            var consumerSp = consumerCollection.BuildServiceProvider();

            consumerSp
                .GetRequiredService<IStoreDatabaseSettings>()
                .StoreConnectionString(connectionString);

            var consumerBootstrapper = consumerSp.GetRequiredService<IBootstrapper>();
            await consumerBootstrapper.BootstrapAsync();

            var consumer = consumerSp.GetRequiredService<TMessageConsumer>();

            var consumerDataStorage = consumerSp.GetRequiredService<SqlServerDataStorageWithFasterRetriesDecorator>();

            await consumerDataStorage.TruncateReceivedAndPublishedTable();

            return consumer;
        }
    }

    public enum CapRunMode
    {
        Serialised,
        Chaos
    }

    private static void CreateDatabaseIfNotExists(string connectionString, string dbName)
    {
        SqlCommand? cmd;
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using (cmd = new SqlCommand($"If(db_id(N'{dbName}') IS NULL) CREATE DATABASE [{dbName}]", connection))
        {
            cmd.ExecuteNonQuery();
        }
    }
}