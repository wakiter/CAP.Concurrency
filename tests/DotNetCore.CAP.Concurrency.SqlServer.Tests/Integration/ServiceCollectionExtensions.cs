using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Concurrency.SqlServer.Tests.Integration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterCAPCommons(this IServiceCollection serviceCollection, int failedRetryInterval, int inFlightTimeInSeconds, bool registerMultiInstanceConcurrency = true)
        {
            serviceCollection.AddLogging(x => x.AddConsole());
            serviceCollection.AddCap(capCfg =>
            {
                capCfg.UseSqlServer(cfg =>
                {
                    cfg.ConnectionString = AppDbContext.ConnectionString;
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
            });
            
            serviceCollection.AddTransient<AppDbContext>();

            if (registerMultiInstanceConcurrency)
            {
                serviceCollection.AddMultiInstanceConcurrency(cfg => cfg.InFlightTime = TimeSpan.FromSeconds(inFlightTimeInSeconds));
            }

            return serviceCollection;
        }
    }
}