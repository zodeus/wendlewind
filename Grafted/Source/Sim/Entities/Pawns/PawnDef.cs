using System;
using JetBrains.Annotations;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim.Entities.Pawns;

[UsedImplicitly]
public class PawnDef : EntityDef {
    public override EntityType EntityType => EntityType.Pawn;
    public BodyDef Body = null!;
    public override Texture2D Icon => throw new NotImplementedException("PawnDef.Icon not implemented, use RaceDef.Icon instead");
}