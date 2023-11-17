using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;

namespace VOEPowerGrid
{
    public class Outpost_PowerGrid_WindTurbine : Outpost_PowerGrid
    {
        public int updateWeatherEveryXTicks = 250;

        private int ticksSinceWeatherUpdate;
        public override void UpdateProducedPower()
        {
            recashProducedPower(ActiveBuildingsCounter.Count() > 0 ? ActiveBuildingsCounter.Sum((ThingDefCountClass tdcc) => -tdcc.thingDef.GetCompProperties<CompProperties_Power>().
#if v1_3
            basePowerConsumption
#elif v1_4
            PowerConsumption
#endif
            * Mathf.Min(Outlet?.Map.windManager.WindSpeed ?? 1f, 1.5f) * tdcc.count) * PowerMultiplier : 0f);
            if (Outlet != null)
            {
#if v1_3
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput1_3();
#elif v1_4
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput();
#endif
            }
        }
        public override void Tick()
        {
            base.Tick();
            ticksSinceWeatherUpdate++;
            if (ticksSinceWeatherUpdate >= updateWeatherEveryXTicks)
            {
                UpdateProducedPower();
                ticksSinceWeatherUpdate = 0;
            }
        }
    }
}
