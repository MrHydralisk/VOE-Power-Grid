using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Outposts;
using Verse;
using UnityEngine;

namespace VOEPowerGrid
{
    public class Outpost_PowerGrid_Refuelable : Outpost_PowerGrid
    {
        public float FuelConsumptionRate => ActiveBuildingsCounter.Sum((ThingDefCountClass tdcc) => (tdcc.thingDef.GetCompProperties<CompProperties_Refuelable>()?.fuelConsumptionRate * tdcc.count) ?? 0);
        public float fuelPowerMultiplier = 0f;
        public float terrainFuelMultiplier = 1f;
        public bool fuelFull = true;

        public override void CalculateTerrainMultiplier()
        {
            base.CalculateTerrainMultiplier();
            terrainFuelMultiplier = 1f;
            Tile tile = Find.WorldGrid[this.Tile];
            float biomeValue = 1f;
            if (PowerGridExt.BiomeFuel.TryGetValue(tile.biome, out biomeValue))
            {
                terrainFuelMultiplier *= biomeValue;
            }
            else 
            {
                terrainFuelMultiplier *= PowerGridExt.BiomeFuelDefault;
            }
            float mult = 1f;
            if (PowerGridExt.HillinessFuel.TryGetValue(tile.hilliness, out mult))
            {
                terrainFuelMultiplier *= mult;
            }
            PowerGridExt.fuelFilter.ResolveReferences();
        }

        public override void UpdateProducedPower()
        {
            RecashProducedPower(ActiveBuildingsCounter.Count() > 0 ? ActiveBuildingsCounter.Sum((ThingDefCountClass tdcc) => -tdcc.thingDef.GetCompProperties<CompProperties_Power>().PowerConsumption * tdcc.count) * terrainPowerMultiplier * fuelPowerMultiplier * PowerMultiplier : 0f);
            if (Outlet != null)
            {
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput();
            }
        }

        public void ConsumeFuel()
        {
            float fuelConsumtionRate = FuelConsumptionRate;
            int fuelCR = (int)(fuelConsumtionRate * (1 - terrainFuelMultiplier));
            Log.Message("1: " + fuelCR.ToString() + " | " + terrainFuelMultiplier.ToString() + " | " + fuelConsumtionRate.ToString());
            if (fuelCR > 0)
            {
                ThingFilter tf = PowerGridExt.fuelFilter;
                Log.Message("2: " + tf.ToString() + " | " + tf.AnyAllowedDef.ToString() + " | " + tf.AllowedDefCount.ToString());
                foreach (ThingDef td in PowerGridExt.fuelFilter.AllowedThingDefs)
                {
                    int availableThings = this.Things.Sum((Thing t) => t.def == td ? t.stackCount : 0);
                    int fuelTaken = Mathf.Min(availableThings, fuelCR);
                    Log.Message("3: " + td.LabelCap + " | " + fuelTaken.ToString() + " | " + availableThings.ToString() + " | " + fuelCR.ToString());
                    if (fuelTaken > 0)
                    {
                        fuelCR -= fuelTaken;
                        List<Thing> usedThings = TakeItems(td, fuelTaken);
                        foreach (Thing t in usedThings)
                        {
                            t.Destroy();
                        }
                        if (fuelCR <= 0)
                        {
                            break;
                        }
                    }
                }
                fuelPowerMultiplier = 1 - (fuelCR / (int)fuelConsumtionRate);
                if (fuelPowerMultiplier >= 1)
                {
                    fuelFull = true;
                }
                else if (fuelFull)
                {
                    Find.LetterStack.ReceiveLetter("VOEPowerGrid.Letters.InsufficientFuel.Label".Translate(Name), "VOEPowerGrid.Letters.InsufficientFuel.Text".Translate(), LetterDefOf.NeutralEvent);
                    fuelFull = false;
                }
            }
            else
            {
                fuelPowerMultiplier = 1;
                fuelFull = true;
            }
            Log.Message("4: " + fuelPowerMultiplier.ToString() + " | " + fuelCR.ToString() + " | " + fuelConsumtionRate.ToString());
            UpdateProducedPower();
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 60000 == 0)
            {
                ConsumeFuel();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref fuelPowerMultiplier, "fuelPowerMultiplier", 0);
        }

        public override string ProductionString()
        {
            List<string> inspectStrings = new List<string>();
            inspectStrings.Add(base.ProductionString());
            if (terrainFuelMultiplier != 0f)
            {
                inspectStrings.Add("VOEPowerGrid.Base.TerrainFuelString".Translate(terrainFuelMultiplier.ToString("F")));
            }
            if (fuelPowerMultiplier != 1f)
            {
                inspectStrings.Add("VOEPowerGrid.Base.FuelPowerString".Translate(fuelPowerMultiplier.ToString("F")));
            }
            inspectStrings.Add("VOEPowerGrid.Base.FuelConsumptionString".Translate((FuelConsumptionRate * (1 - terrainFuelMultiplier)).ToString(), PowerGridExt.fuelFilter.Summary));
            return string.Join("\n", inspectStrings);
        }
    }
}
