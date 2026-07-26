using Microsoft.Extensions.DependencyInjection;

namespace Scriptorium.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScriptoriumCore(this IServiceCollection services)
    {
        return services;
    }
}
