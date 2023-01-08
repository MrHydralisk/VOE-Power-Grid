using System.Collections.Generic;
using RimWorld;
using Outposts;
using Verse;

namespace VOEPowerGrid
{
    public class Outpost_PowerGrid_Tidal : Outpost_PowerGrid
    {
        public static string CanSpawnOnWith(int tile, List<Pawn> pawns)
        {
            return (Find.World.CoastDirectionAt(tile) == Rot4.Invalid) ? "VOEPowerGrid.MustBeMade.Coast".Translate() : ((TaggedString)null);
        }

        public static string RequirementsString(int tile, List<Pawn> pawns)
        {
            return "VOEPowerGrid.MustBeMade.Coast".Translate().Requirement(Find.World.CoastDirectionAt(tile) == Rot4.Invalid);
        }
    }
}
