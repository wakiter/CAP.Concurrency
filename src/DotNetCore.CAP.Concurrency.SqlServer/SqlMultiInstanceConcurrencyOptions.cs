using System;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCore.CAP.Concurrency.SqlServer
{
    internal sealed class SqlMultiInstanceConcurrencyOptions : ICapOptionsExtension
    {
        private readonly Action<SqlServerConcurrencyOptions> _sqlServerConcurrencyOptions;

        public SqlMultiInstanceConcurrencyOptions(Action<SqlServerConcurrencyOptions>? sqlServerConcurrencyOptions)
        {
            _sqlServerConcurrencyOptions = sqlServerConcurrencyOptions ?? (opts => { });
        }

        public void AddServices(IServiceCollection services)
        {
            var sqlServerOptions = new SqlServerConcurrencyOptions();

            _sqlServerConcurrencyOptions.Invoke(sqlServerOptions);

            services
                .AddMultiInstanceConcurrencyCommons(sqlServerOptions);

            services
                .TryAddSingleton(sqlServerOptions);

            services
                .Decorate<IStorageInitializer, SqlServerConcurrencyStorageInitializerDecorator>();
            services
                .Decorate<ISubscribeDispatcher, ConcurrencySubscribeDispatcherDecorator>();

            services
                .TryAddSingleton<ITryMarkingMessageAsInFlight, MessageAsInFlightMarkerTrier>();
            services
                .TryAddSingleton<IRemoveInFlightMarkFromMessage, InFlightMarkFromMessageRemover>();
        }
    }
}