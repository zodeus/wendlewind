using Microsoft.Extensions.DependencyInjection;

namespace Wendlemire.Sim;

public sealed class SimFactory : ISimFactory
{
    private readonly GameContext _context;
    private IServiceProvider? _services;

    public SimFactory(GameContext context, IServiceProvider? services = null)
    {
        _context = context;
        _services = services;
    }

    public void AttachServices(IServiceProvider services) => _services = services;

    public T Create<T>(Type type) where T : class => Create<T>(type, Array.Empty<object>());

    public T Create<T>(Type type, params object[] ctorArgs) where T : class =>
        (T)Create(type, ctorArgs ?? Array.Empty<object>());

    public object Create(Type type, object[] ctorArgs)
    {
        object instance;
        if (_services != null)
        {
            instance = ctorArgs.Length == 0
                ? ActivatorUtilities.CreateInstance(_services, type)
                : ActivatorUtilities.CreateInstance(_services, type, ctorArgs);
        }
        else
        {
            instance = CreateWithoutServices(type, ctorArgs);
        }

        Bind(instance);
        return instance;
    }

    private object CreateWithoutServices(Type type, object[] ctorArgs)
    {
        if (ctorArgs.Length > 0)
        {
            return Activator.CreateInstance(type, ctorArgs)!;
        }

        try
        {
            return Activator.CreateInstance(type, _context.SimRng)!;
        }
        catch (MissingMethodException)
        {
            return Activator.CreateInstance(type)!;
        }
    }

    public T CreateEntity<T>(EntityDef def, bool suppressInitialization = false) where T : Entity
    {
        var entity = Create<T>(def.EntityClass);
        entity.Id = _context.IdProvider.NextEntityId();
        entity.Def = def;
        if (!suppressInitialization)
        {
            entity.Initialize();
        }

        return entity;
    }

    public T CreateEntity<T>(ItemDef def, int stackSize) where T : Item
    {
        var entity = CreateEntity<T>(def);
        if (entity.IsStackable == false && stackSize != 1)
        {
            Log.Error($"Tried to create entity with StackSize of {stackSize} but {entity} is not stackable, setting StackSize to 1");
            entity.StackSize = 1;
        }
        else if (stackSize > def.StackLimit)
        {
            Log.Error($"Tried to create entity with StackSize of {stackSize} but {entity} StackLimit is {def.StackLimit}, setting StackSize to {def.StackLimit}");
            entity.StackSize = def.StackLimit;
        }
        else
        {
            entity.StackSize = stackSize;
        }

        return entity;
    }

    public BodyPartModifier CreateModifier(BodyPartModifierDef def, int duration, double power)
    {
        var modifier = Create<BodyPartModifier>(def.HandlerClass);
        modifier.Def = def;
        modifier.Id = _context.IdProvider.NextBodyPartModifierId();
        modifier.DurationInTicks = duration;
        modifier.Power = power;
        modifier.Initialize();
        return modifier;
    }

    public void Bind(object? instance)
    {
        if (instance == null)
        {
            return;
        }

        if (instance is IHasContext hasContext)
        {
            hasContext.Context = _context;
        }

        if (instance is IHasRng hasRng)
        {
            hasRng.Rng ??= _context.SimRng;
        }
    }

    public void RebindGraph()
    {
        Bind(_context);
        Bind(_context.World);
        Bind(_context.DeathRecords);
        Bind(_context.Achievements);
        if (_context.World == null)
        {
            return;
        }

        Bind(_context.World.Player);
        Bind(_context.World.ProgressTracker);
        if (_context.World.Player != null)
        {
            BindPawn(_context.World.Player.Pawn);
        }

        foreach (var zone in _context.World.Zones)
        {
            Bind(zone);
            if (zone.ActiveEncounter != null)
            {
                BindEncounter(zone.ActiveEncounter);
            }
        }
    }

    private void BindEncounter(Encounter encounter)
    {
        Bind(encounter);
        Bind(encounter.CombatHandler);
        foreach (var pawn in encounter.PlayerPawns)
        {
            BindPawn(pawn);
        }

        foreach (var pawn in encounter.EnemyPawns)
        {
            BindPawn(pawn);
        }
    }

    private void BindPawn(Pawn? pawn)
    {
        if (pawn == null)
        {
            return;
        }

        Bind(pawn);
        if (pawn.Body != null)
        {
            Bind(pawn.Body);
            Bind(pawn.Body.Handler);
            foreach (var part in pawn.Body.AllParts)
            {
                Bind(part);
                foreach (var modifier in part.Modifiers)
                {
                    Bind(modifier);
                }

                foreach (var item in part.Equipment.Values)
                {
                    BindItem(item);
                }
            }
        }

        if (pawn.Inventory != null)
        {
            foreach (var entity in pawn.Inventory)
            {
                if (entity is Item item)
                {
                    BindItem(item);
                }
            }
        }

        if (pawn.Equipment != null)
        {
            foreach (var item in pawn.Equipment)
            {
                BindItem(item);
            }
        }
    }

    private void BindItem(Item? item)
    {
        if (item == null)
        {
            return;
        }

        Bind(item);
        Bind(item.EnchantmentHandler);
        Bind(item.TrinketHandler);
        Bind(item.EquipmentHandler);
        Bind(item.PotionHandler);
        Bind(item.WeaponHandler);
        if (item.Enchantments != null)
        {
            foreach (var enchantment in item.Enchantments)
            {
                BindItem(enchantment);
            }
        }
    }
}
