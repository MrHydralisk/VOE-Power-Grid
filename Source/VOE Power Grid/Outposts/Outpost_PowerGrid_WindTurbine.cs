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
            RecashProducedPower(ActiveBuildingsCounter.Count() > 0 ? ActiveBuildingsCounter.Sum((ThingDefCountClass tdcc) => -tdcc.thingDef.GetCompProperties<CompProperties_Power>().PowerConsumption * Mathf.Min(Outlet?.Map.windManager.WindSpeed ?? 1f, 1.5f) * tdcc.count) * terrainPowerMultiplier * PowerMultiplier : 0f);
            if (Outlet != null)
            {
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput();
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
