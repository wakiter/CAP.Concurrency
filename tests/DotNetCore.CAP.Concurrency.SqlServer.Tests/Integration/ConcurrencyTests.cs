using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration;

public sealed class ConcurrencyTests
{
    private readonly ITestOutputHelper _outputHelper;

    public ConcurrencyTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task multi_instances_are_serialised_when_extension_is_applied()
    {
        var publisherCollection = new ServiceCollection();
        publisherCollection.RegisterCAPCommons(60, 60);

        var publisherSp = publisherCollection.BuildServiceProvider();
        await publisherSp.GetRequiredService<IBootstrapper>().BootstrapAsync();

        var publisher = publisherSp.GetRequiredService<ICapPublisher>();

        var consumerOneCollection = new ServiceCollection();
        var consumerTwoCollection = new ServiceCollection();

        consumerOneCollection.RegisterCAPCommons(5, 7);
        consumerTwoCollection.RegisterCAPCommons(5, 7);

        consumerOneCollection.AddSingleton<EventConsumer>();
        consumerTwoCollection.AddSingleton<EventConsumer>();

        consumerOneCollection.Decorate<IDataStorage, SqlServerDataStorageWithFasterRetriesDecorator>();
        consumerTwoCollection.Decorate<IDataStorage, SqlServerDataStorageWithFasterRetriesDecorator>();

        consumerOneCollection
            .AddSingleton(sp => (SqlServerDataStorageWithFasterRetriesDecorator) sp.GetRequiredService<IDataStorage>());

        consumerTwoCollection
            .AddSingleton(sp => (SqlServerDataStorageWithFasterRetriesDecorator)sp.GetRequiredService<IDataStorage>());

        consumerOneCollection.AddLogging(x => x.AddXUnit(_outputHelper));
        consumerTwoCollection.AddLogging(x => x.AddXUnit(_outputHelper));

        var consumerOneSp = consumerOneCollection.BuildServiceProvider();
        var consumerTwoSp = consumerTwoCollection.BuildServiceProvider();

        var consumerOneBootstrapper = consumerOneSp.GetRequiredService<IBootstrapper>();
        var consumerTwoBootstrapper = consumerTwoSp.GetRequiredService<IBootstrapper>();

        var consumerOne = consumerOneSp.GetRequiredService<EventConsumer>();
        var consumerTwo = consumerTwoSp.GetRequiredService<EventConsumer>();

        var consumerOneDataStorage = consumerOneSp.GetRequiredService<SqlServerDataStorageWithFasterRetriesDecorator>();

        await consumerOneDataStorage.TruncateReceivedAndPublishedTable();

        Task.WaitAll(consumerOneBootstrapper.BootstrapAsync(), consumerTwoBootstrapper.BootstrapAsync());

        var publishedEvent = new Event();

        await publisher.PublishAsync("Consumer_Event", publishedEvent);

        await Task.Delay(TimeSpan.FromSeconds(25));

        var overallInvocationCount = consumerOne.ReceivedEvents.Sum(x => x.Value) 
                                     + consumerTwo.ReceivedEvents.Sum(x => x.Value);

        overallInvocationCount.Should().Be(5);
    }

    [Fact]
    public async Task multi_instances_are_executed_in_a_concurrent_way_when_extension_is_not_applied()
    {
        var publisherCollection = new ServiceCollection();
        publisherCollection.RegisterCAPCommons(60, 60);

        var publisherSp = publisherCollection.BuildServiceProvider();
        await publisherSp.GetRequiredService<IBootstrapper>().BootstrapAsync();

        var publisher = publisherSp.GetRequiredService<ICapPublisher>();

        var consumerOneCollection = new ServiceCollection();
        var consumerTwoCollection = new ServiceCollection();

        consumerOneCollection.RegisterCAPCommons(5, 7, false);
        consumerTwoCollection.RegisterCAPCommons(5, 7, false);

        consumerOneCollection.AddSingleton<EventConsumer>();
        consumerTwoCollection.AddSingleton<EventConsumer>();

        consumerOneCollection.Decorate<IDataStorage, SqlServerDataStorageWithFasterRetriesDecorator>();
        consumerTwoCollection.Decorate<IDataStorage, SqlServerDataStorageWithFasterRetriesDecorator>();

        consumerOneCollection
            .AddSingleton(sp => (SqlServerDataStorageWithFasterRetriesDecorator)sp.GetRequiredService<IDataStorage>());

        consumerTwoCollection
            .AddSingleton(sp => (SqlServerDataStorageWithFasterRetriesDecorator)sp.GetRequiredService<IDataStorage>());

        consumerOneCollection.AddLogging(x => x.AddXUnit(_outputHelper));
        consumerTwoCollection.AddLogging(x => x.AddXUnit(_outputHelper));

        var consumerOneSp = consumerOneCollection.BuildServiceProvider();
        var consumerTwoSp = consumerTwoCollection.BuildServiceProvider();

        var consumerOneBootstrapper = consumerOneSp.GetRequiredService<IBootstrapper>();
        var consumerTwoBootstrapper = consumerTwoSp.GetRequiredService<IBootstrapper>();

        var consumerOne = consumerOneSp.GetRequiredService<EventConsumer>();
        var consumerTwo = consumerTwoSp.GetRequiredService<EventConsumer>();

        var consumerOneDataStorage = consumerOneSp.GetRequiredService<SqlServerDataStorageWithFasterRetriesDecorator>();

        await consumerOneDataStorage.TruncateReceivedAndPublishedTable();

        Task.WaitAll(consumerOneBootstrapper.BootstrapAsync(), consumerTwoBootstrapper.BootstrapAsync());

        var publishedEvent = new Event();

        await publisher.PublishAsync("Consumer_Event", publishedEvent);

        await Task.Delay(TimeSpan.FromSeconds(25));

        var overallInvocationCount = consumerOne.ReceivedEvents.Sum(x => x.Value)
                                     + consumerTwo.ReceivedEvents.Sum(x => x.Value);

        overallInvocationCount.Should().NotBe(5); //it shows that this is barely predictable
    }
}