using System;

namespace Grafted.Sim.Entities.Pawns;

[Flags]
public enum PawnType : byte {
    Player,
    Enemy,
    Npc
}