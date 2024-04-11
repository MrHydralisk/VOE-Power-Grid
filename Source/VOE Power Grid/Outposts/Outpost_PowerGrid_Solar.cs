using Verse;
using System.Linq;
using UnityEngine;

namespace VOEPowerGrid
{
    public class Outpost_PowerGrid_Solar : Outpost_PowerGrid
    {
        public int updateSkyEveryXTicks = 250;

        private int ticksSinceSkyUpdate;
        public override void UpdateProducedPower()
        {
            RecashProducedPower(ActiveBuildingsCounter.Count() > 0 ? ActiveBuildingsCounter.Sum((ThingDefCountClass tdcc) => Mathf.Lerp(GetConstructionOption(tdcc.thingDef).NightPower, GetConstructionOption(tdcc.thingDef).FullSunPower, Outlet?.Map.skyManager.CurSkyGlow ?? 1f) * tdcc.count) * PowerMultiplier : 0f);
            if (Outlet != null)
            {
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput();
            }
        }
        public override float MaxPowerProducedByBuilding(ConstructionOption co)
        {
            return co.FullSunPower * terrainPowerMultiplier * PowerMultiplier;
        }
        public override void Tick()
        {
            base.Tick();
            ticksSinceSkyUpdate++;
            if (ticksSinceSkyUpdate >= updateSkyEveryXTicks)
            {
                UpdateProducedPower();
                ticksSinceSkyUpdate = 0;
            }
        }
    }
}
