using Microsoft.Extensions.DependencyInjection;

namespace Wendlemire.Sim;

public static class SimServices
{
    public static IServiceCollection AddWendlemireSimulation(this IServiceCollection services)
    {
        services.AddScoped<GameContext>(sp =>
        {
            var context = new GameContext();
            context.AttachServices(sp);
            return context;
        });
        services.AddScoped<ISimFactory>(sp => sp.GetRequiredService<GameContext>().Factory);
        services.AddScoped<IRng>(sp => sp.GetRequiredService<GameContext>().SimRng);
        SimTypeCatalog.Register(services);
        return services;
    }

    public static ServiceProvider BuildRoot()
    {
        var services = new ServiceCollection();
        services.AddWendlemireSimulation();
        return services.BuildServiceProvider();
    }
}
