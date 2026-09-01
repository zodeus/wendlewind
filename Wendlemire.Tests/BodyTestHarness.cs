using Microsoft.Extensions.DependencyInjection;
using Wendlemire.Definitions;
using Wendlemire.Sim;
using Wendlemire.Sim.Combat;
using Wendlemire.Sim.Entities.Items;
using Wendlemire.Sim.Entities.Pawns;

namespace Wendlemire.Tests;

/// <summary>
/// Shared Sim setup for body/blood unit tests: hydrate a pawn, find parts, and apply seeded damage.
/// </summary>
internal sealed class BodyTestHarness : IDisposable
{
    private readonly ServiceProvider _root;
    private readonly IServiceScope _scope;

    public GameContext Context { get; }
    public Pawn Pawn { get; }

    private BodyTestHarness(string pawnDefMoniker, int seed = 1)
    {
        TestData.EnsureLoaded();
        _root = SimServices.BuildRoot();
        _scope = _root.CreateScope();
        Context = _scope.ServiceProvider.GetRequiredService<GameContext>();
        Context.Rng = new Random(seed);
        Pawn = CreatePawn(pawnDefMoniker, "Subject");
    }

    public static BodyTestHarness Human(int seed = 1) => new("HumanA", seed);

    public static BodyTestHarness Ghoul(int seed = 1) => new("Ghoul", seed);

    public Pawn CreatePawn(string pawnDefMoniker, string name)
    {
        var def = DefRepository<PawnDef>.GetByMoniker(pawnDefMoniker)
                  ?? throw new InvalidOperationException($"Missing pawn def '{pawnDefMoniker}'.");
        var loadout = DefRepository<PawnLoadoutDef>.GetByMoniker("EmptyLoadout")
                      ?? throw new InvalidOperationException("Missing EmptyLoadout.");
        return PawnGenerator.CreatePawn(Context, new PawnRequest(name, def, loadout, PawnType.Enemy));
    }

    public BodyPart Part(BodyPartType type) =>
        Pawn.Body.AllParts.First(p => p.Type == type);

    public BodyPart External(BodyPartType type) =>
        Pawn.Body.AllExternalParts.First(p => p.Type == type && !p.IsDestroyed);

    public IReadOnlyList<BodyPart> Parts(BodyPartType type) =>
        Pawn.Body.AllParts.Where(p => p.Type == type).ToList();

    public static DamageContext Damage(double amount, Func<SubstanceType, float>? substanceModifier = null) =>
        new(amount, DamageType.Sharp, "test", [], substanceModifier);

    public List<DamagedBodyPartRecord> Apply(BodyPart part, double amount, bool cascade = true)
    {
        var damaged = new List<DamagedBodyPartRecord>();
        part.ApplyDamage(Damage(amount), damaged, cascade);
        return damaged;
    }

    public void UseAlwaysHitRng() => Context.Rng = new ScriptedRandom(fallback: 0.99, 0.0, 0.99);

    public void UseChanceSuccessRng() => Context.Rng = new ScriptedRandom(0.0);

    public void UseChanceFailRng() => Context.Rng = new ScriptedRandom(0.99);

    public void TickBody(int times = 1)
    {
        for (var i = 0; i < times; i++)
        {
            Pawn.Body.Tick();
        }
    }

    public Item CreateWeapon(string moniker = "IronSword")
    {
        var def = DefRepository<ItemDef>.GetByMoniker(moniker)
                  ?? throw new InvalidOperationException($"Missing item '{moniker}'.");
        return Context.Factory.CreateEntity<Item>(def);
    }

    public void Strike(Pawn attacker, BodyPart target, double amount)
    {
        var weapon = CreateWeapon();
        var maneuver = weapon.ItemDef.WeaponProperties!.WeaponManeuvers[0];
        var request = new DamageRequest(attacker, weapon, maneuver)
        {
            TargetedPart = target
        };
        request.RawDamages.Add(new Damage(weapon, amount, maneuver.Label));
        target.Body!.Pawn.TakeDamage(request);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _root.Dispose();
    }
}

/// <summary>
/// Random source that returns scripted <see cref="Random.NextDouble"/> values so Chance() is deterministic.
/// </summary>
internal sealed class ScriptedRandom : Random
{
    private readonly Queue<double> _values;
    private readonly double _fallback;

    public ScriptedRandom(double fallback, params double[] values) : base(1)
    {
        _fallback = fallback;
        _values = new Queue<double>(values);
    }

    public override double NextDouble() => _values.Count > 0 ? _values.Dequeue() : _fallback;
}
