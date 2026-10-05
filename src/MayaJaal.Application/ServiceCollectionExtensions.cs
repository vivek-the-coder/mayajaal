using Microsoft.Extensions.DependencyInjection;

namespace MayaJaal.Application;

/// <summary>
/// Thin application-layer registration that forwards to Infrastructure DI.
/// Prefer <c>MayaJaal.Infrastructure.DependencyInjection.AddMayaJaalCore</c> directly when convenient.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMayaJaalCore(this IServiceCollection services)
        => MayaJaal.Infrastructure.DependencyInjection.AddMayaJaalCore(services);
}
