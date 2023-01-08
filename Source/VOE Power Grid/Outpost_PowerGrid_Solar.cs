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
            recashProducedPower(BuildingsCounter.Count() > 0 ? BuildingsCounter.Sum((ThingDefCountClass tdcc) => Mathf.Lerp(GetConstructionOption(tdcc.thingDef).NightPower, GetConstructionOption(tdcc.thingDef).FullSunPower, Outlet?.Map.skyManager.CurSkyGlow ?? 1f) * tdcc.count) * PowerMultiplier : 0f);
            if (Outlet != null)
            {
#if v1_3
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput1_3();
#elif v1_4
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput();
#endif
            }
        }
        public override float MaxPowerProducedByBuilding(ConstructionOption co)
        {
            return -co.FullSunPower * PowerMultiplier;
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
