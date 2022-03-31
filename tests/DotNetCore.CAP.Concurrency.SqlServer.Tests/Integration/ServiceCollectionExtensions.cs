using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterCapCommons(this IServiceCollection serviceCollection, int failedRetryInterval, int inFlightTimeInSeconds, bool registerMultiInstanceConcurrency, string connectionString)
        {
            serviceCollection.AddLogging(x => x.AddConsole());
            serviceCollection.AddCap(capCfg =>
            {
                capCfg.UseSqlServer(cfg =>
                {
                    cfg.ConnectionString = connectionString;
                });

                capCfg.UseRabbitMQ(x =>
                {
                    x.HostName = "localhost";
                    x.UserName = "guest";
                    x.Password = "guest";
                    x.ExchangeName = "wakiter";
                    x.VirtualHost = "/";
                });

                capCfg.FailedRetryInterval = failedRetryInterval;
                capCfg.UseEntityFramework<AppDbContext>();

                if (registerMultiInstanceConcurrency)
                {
                    capCfg.UseSqlMultiInstanceConcurrency(cfg => cfg.InFlightTime = TimeSpan.FromSeconds(inFlightTimeInSeconds));
                }
            });

            serviceCollection.AddTransient<AppDbContext>();
            serviceCollection.AddSingleton<DatabaseSettingsStorage>();
            serviceCollection.AddSingleton<IKeepDatabaseSettings>(sp => sp.GetRequiredService<DatabaseSettingsStorage>());
            serviceCollection.AddSingleton<IStoreDatabaseSettings>(sp => sp.GetRequiredService<DatabaseSettingsStorage>());

            //if (registerMultiInstanceConcurrency)
            //{
            //    serviceCollection.AddMultiInstanceConcurrency(cfg => cfg.InFlightTime = TimeSpan.FromSeconds(inFlightTimeInSeconds));
            //}

            return serviceCollection;
        }
    }
}