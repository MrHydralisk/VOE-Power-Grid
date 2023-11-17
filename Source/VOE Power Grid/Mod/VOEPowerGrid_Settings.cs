using Verse;

namespace VOEPowerGrid
{
    public class VOEPowerGrid_Settings : ModSettings
    {
        public float BuildingCapacityPerSkill = 1f;
        public int BaseBuildingCapacity = 0;
        public int PowerLossPerTiles = 2;
        public int TransmissionTowerRange = 1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref BuildingCapacityPerSkill, "BuildingCapacityPerSkill", defaultValue: 1f);
            Scribe_Values.Look(ref BaseBuildingCapacity, "BaseBuildingCapacity", defaultValue: 0);
            Scribe_Values.Look(ref PowerLossPerTiles, "PowerLossPerTiles", defaultValue: 2);
            Scribe_Values.Look(ref TransmissionTowerRange, "TransmissionTowerRange", defaultValue: 1);
        }
    }
}