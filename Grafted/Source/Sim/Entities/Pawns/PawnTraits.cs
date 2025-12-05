using System.Collections;

namespace Grafted.Sim.Entities.Pawns {
    public class TraitDef : Def { }

    public class PawnTraits : IEnumerable<TraitDef>, IExposable {
        private List<TraitDef> _traits = new();

        public PawnTraits(Pawn pawn) { }

        public void Add(TraitDef trait) {
            if (HasTrait(trait)) return;
            _traits.Add(trait);
        }
        
        public bool HasTrait(TraitDef trait) {
            return _traits.Contains(trait);
        }

        public IEnumerator<TraitDef> GetEnumerator()
        {
            return ((IEnumerable<TraitDef>)_traits).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void ExposeData() {
            ScribeCollections.Look(ref _traits!, "Traits", LookMode.Def);
        }
    }
}