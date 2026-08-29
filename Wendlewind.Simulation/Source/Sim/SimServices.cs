using Microsoft.Extensions.DependencyInjection;

namespace Wendlewind.Sim;

public static class SimServices
{
    public static IServiceCollection AddWendlewindSimulation(this IServiceCollection services)
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
        services.AddWendlewindSimulation();
        return services.BuildServiceProvider();
    }
}
