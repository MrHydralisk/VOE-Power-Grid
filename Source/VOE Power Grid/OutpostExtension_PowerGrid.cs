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
        public Dictionary<Hilliness, float> HillinessFuel = new Dictionary<Hilliness, float>();
        public Dictionary<RiverDef, float> RiverCapacity = new Dictionary<RiverDef, float>();
        public Dictionary<BiomeDef, float> BiomeCapacity = new Dictionary<BiomeDef, float>();
        public Dictionary<BiomeDef, float> BiomePower = new Dictionary<BiomeDef, float>();
        public Dictionary<BiomeDef, float> BiomeFuel = new Dictionary<BiomeDef, float>();
        public float BiomeFuelDefault = 0f;
        public bool CanBuildOnWater = false;
        public bool CanBuildOnCoast = false;
        public ThingFilter fuelFilter = new ThingFilter();
    }
}
