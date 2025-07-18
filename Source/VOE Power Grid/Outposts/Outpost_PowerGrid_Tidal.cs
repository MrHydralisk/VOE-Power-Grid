using System.Collections.Generic;
using RimWorld;
using Outposts;
using Verse;
using RimWorld.Planet;

namespace VOEPowerGrid
{
    public class Outpost_PowerGrid_Tidal : Outpost_PowerGrid
    {
        public override void CalculateTerrainMultiplier()
        {
            base.CalculateTerrainMultiplier();
            List<PlanetTile> tmpNeighbors = new List<PlanetTile>();
            Find.WorldGrid.GetTileNeighbors(this.Tile, tmpNeighbors);
            foreach (int neighbor in tmpNeighbors)
            {
                BiomeDef bd = Find.WorldGrid[neighbor].PrimaryBiome;
                float mult = 1f;
                if (PowerGridExt.BiomePower.TryGetValue(bd, out mult))
                {
                    terrainPowerMultiplier *= mult;
                    break;
                }
            }
        }

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
