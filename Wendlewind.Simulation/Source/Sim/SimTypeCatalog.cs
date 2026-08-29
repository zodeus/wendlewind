using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Wendlewind;
using Wendlewind.Sim.Achievements.Handlers;
using Wendlewind.Sim.Entities.Pawns.Bodies;
using Wendlewind.Sim.Entities.Pawns.Bodies.Handlers;

/// <summary>
/// Registers constructible sim types as transient so the run scope can inject IRng/GameContext.
/// Types that need parent/def ctor args (Encounter, CombatHandler, DefaultStatHandler) stay
/// out of the container and are created through <see cref="ISimFactory"/> with those args.
/// </summary>
public static class SimTypeCatalog
{
    private static readonly Type[] InjectedServices =
    [
        typeof(IRng),
        typeof(GameContext),
        typeof(ISimFactory),
        typeof(IServiceProvider)
    ];

    public static void Register(IServiceCollection services)
    {
        var assembly = typeof(GameContext).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                continue;
            }

            if (ShouldRegister(type))
            {
                services.AddTransient(type);
            }
        }
    }

    private static bool ShouldRegister(Type type)
    {
        if (typeof(GameContext).IsAssignableFrom(type))
        {
            return false;
        }

        if (!IsHandlerFamily(type))
        {
            return false;
        }

        return HasResolvableConstructor(type);
    }

    private static bool IsHandlerFamily(Type type)
    {
        return typeof(IHasRng).IsAssignableFrom(type)
               || typeof(IBodyGenerator).IsAssignableFrom(type)
               || typeof(AchievementHandler).IsAssignableFrom(type)
               || typeof(Entity).IsAssignableFrom(type)
               || typeof(BodyPartModifier).IsAssignableFrom(type)
               || typeof(DefaultBodyHandler).IsAssignableFrom(type);
    }

    private static bool HasResolvableConstructor(Type type)
    {
        return type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .Any(ctor => ctor.GetParameters().All(parameter =>
                parameter.HasDefaultValue
                || parameter.IsOptional
                || InjectedServices.Any(service => service.IsAssignableFrom(parameter.ParameterType))));
    }
}
