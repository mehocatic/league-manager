using LeagueManager.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueManager.Application;

public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces = [typeof(IQueryHandler<,>), typeof(ICommandHandler<,>)];

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // In-process cache is enough for a single API instance; a second instance would need a distributed cache.
        services.AddMemoryCache();

        // Every handler is registered against its handler interface by scanning this assembly, so a new use case
        // needs no DI wiring. This is the one job MediatR would otherwise do for us.
        var registrations =
            from type in typeof(DependencyInjection).Assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition())
            select (contract, type);

        foreach (var (contract, implementation) in registrations)
        {
            // Scoped, because handlers use the DbContext, which is itself scoped to one HTTP request.
            services.AddScoped(contract, implementation);
        }

        return services;
    }
}
