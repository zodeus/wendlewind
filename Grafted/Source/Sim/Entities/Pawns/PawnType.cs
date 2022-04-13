using System;

namespace Grafted.Sim.Entities.Pawns;

[Flags]
public enum PawnType : byte {
    Invalid,
    Player,
    Enemy,
    
}