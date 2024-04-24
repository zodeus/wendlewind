using System.Collections;

namespace Grafted.Sim.Entities.Pawns {
    public class TraitDef : Def { }

    public class PawnTraits : IEnumerable<TraitDef>, IExposable {
        private List<TraitDef> _traits = new();

        public PawnTraits(Pawn pawn) { }

        public void Add(TraitDef trait) {
            _traits.Add(trait);
        }

        public IEnumerator<TraitDef> GetEnumerator() {
            return ((IEnumerable<TraitDef>) _traits).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void ExposeData() {
            Scribe_Collections.Look(ref _traits!, "Traits", LookMode.Def);
        }
    }
}