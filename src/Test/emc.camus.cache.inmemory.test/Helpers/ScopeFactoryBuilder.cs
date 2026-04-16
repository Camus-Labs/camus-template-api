using emc.camus.application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace emc.camus.cache.inmemory.test.Helpers;

internal static class ScopeFactoryBuilder
{
    public static IServiceScopeFactory Create(IGeneratedTokenRepository? repository)
    {
        var services = new ServiceCollection();
        if (repository != null)
        {
            services.AddSingleton(repository);
        }
        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }
}
