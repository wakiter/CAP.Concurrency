using System;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCore.CAP.Concurrency.SqlServer
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMultiInstanceConcurrency(
            this IServiceCollection serviceCollection,
            Action<SqlServerConcurrencyOptions>? cfg = null)
        {
            var sqlServerOptions = new SqlServerConcurrencyOptions();

            cfg?.Invoke(sqlServerOptions);

            serviceCollection
                .AddMultiInstanceConcurrencyCommons(sqlServerOptions);

            serviceCollection
                .TryAddSingleton(sqlServerOptions);

            serviceCollection
                .Decorate<IStorageInitializer, SqlServerConcurrencyStorageInitializerDecorator>();
            serviceCollection
                .Decorate<ISubscribeDispatcher, ConcurrencySubscribeDispatcherDecorator>();

            serviceCollection
                .TryAddSingleton<ITryMarkingMessageAsInFlight, MessageAsInFlightMarkerTrier>();
            serviceCollection
                .TryAddSingleton<IRemoveInFlightMarkFromMessage, InFlightMarkFromMessageRemover>();

            return serviceCollection;
        }
    }
}
