using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace VOEPowerGrid
{
    public class OutpostExtension_PowerGrid : DefModExtension
    {
        public List<ConstructionOption> ConstructionOptions;
        public Dictionary<Hilliness, float> HillinessCapacity = new Dictionary<Hilliness, float>();
        public Dictionary<Hilliness, float> HillinessPower = new Dictionary<Hilliness, float>();
        public Dictionary<RiverDef, float> RiverCapacity = new Dictionary<RiverDef, float>();
        public Dictionary<BiomeDef, float> BiomeCapacity = new Dictionary<BiomeDef, float>();
        public Dictionary<BiomeDef, float> BiomePower = new Dictionary<BiomeDef, float>();
        public bool CanBuildOnWater = false;
        public bool CanBuildOnCoast = false;
    }
}
