using Microsoft.Extensions.DependencyInjection;

namespace PoliceMP.Server.Services.Callouts
{
    public static class ServiceCollectionExtension
    {
        public static ICalloutManagerBuilder AddCalloutManager(this IServiceCollection serviceCollection)
        {
            return new CalloutManagerBuilder(serviceCollection);
        }
    }
}