using System.Collections;

namespace Wendlemire.Sim.Entities.Pawns;

public class PawnBodyEffects : IEnumerable<BodyEffect>, IExposable
{
    private List<BodyEffect> _effects = new();

    public PawnBodyEffects(Pawn pawn)
    {
    }

    IEnumerator<BodyEffect> IEnumerable<BodyEffect>.GetEnumerator()
    {
        return _effects.GetEnumerator();
    }

    public IEnumerator GetEnumerator()
    {
        return _effects.GetEnumerator();
    }

    public void TryApplyEffect(BodyEffect effect)
    {
        if (_effects.Find(e => e.Def == effect.Def) is { } existingEffect)
        {
            existingEffect.TicksLeft += effect.TicksLeft;
            existingEffect.Power += effect.Power;
            if (effect.LastsWholeEncounter)
            {
                existingEffect.LastsWholeEncounter = true;
            }
            return;
        }

        _effects.Add(effect);
    }

    public void ClearWholeEncounterEffects()
    {
        _effects.RemoveAll(e => e.LastsWholeEncounter);
    }

    public bool TryRemove(BodyEffectDef effect)
    {
        return _effects.RemoveAll(e => e.Def == effect) > 0;
    }

    public void SyncEncounterStacks(IReadOnlyDictionary<BodyEffectDef, float> stacks)
    {
        for (var i = _effects.Count - 1; i >= 0; i--)
        {
            var effect = _effects[i];
            if (!effect.LastsWholeEncounter)
            {
                continue;
            }

            if (stacks.TryGetValue(effect.Def, out var power) && power > 0f)
            {
                effect.Power = power;
            }
            else
            {
                _effects.RemoveAt(i);
            }
        }
    }

    public void Tick()
    {
        for (int index = _effects.Count - 1; index >= 0; index--)
        {
            BodyEffect effect = _effects[index];
            effect.Tick();
            if (effect.IsExpired)
            {
                _effects.Remove(effect);
            }
        }
    }

    public void ExposeData()
    {
        ScribeCollections.Look(ref _effects!, "_effects", LookMode.Deep);
    }

    public bool Has(BodyEffectDef effect)
    {
        return _effects.Any(e => e.Def == effect && e.IsExpired == false);
    }
}