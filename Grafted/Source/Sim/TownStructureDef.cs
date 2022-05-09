using System;
using Grafted.Definitions;
using JetBrains.Annotations;

namespace Grafted.Sim;

public class TownStructureDef : Def {
    [UsedImplicitly] public Type StructureClass = typeof(TownStructure);
}