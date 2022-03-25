using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCore.CAP.Concurrency
{
    public static class ServiceCollectionExtensions
    {
        internal static IServiceCollection AddMultiInstanceConcurrencyCommons(
            this IServiceCollection serviceCollection, 
            ConcurrencyOptions options)
        {
            serviceCollection.TryAddSingleton(options);

            return serviceCollection;
        }
    }
}
