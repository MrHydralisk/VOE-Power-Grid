using System.Collections.Generic;
using RimWorld;
using Outposts;
using Verse;

namespace VOEPowerGrid
{
    public class Outpost_PowerGrid_Watermill : Outpost_PowerGrid
    {
        public static string CanSpawnOnWith(int tile, List<Pawn> pawns)
        {
            return (Find.WorldGrid[tile].Rivers.NullOrEmpty()) ? "VOEPowerGrid.MustBeMade.River".Translate() : ((TaggedString)null);
        }

        public static string RequirementsString(int tile, List<Pawn> pawns)
        {
            return "VOEPowerGrid.MustBeMade.River".Translate().Requirement(Find.WorldGrid[tile].Rivers.NullOrEmpty());
        }
    }
}
