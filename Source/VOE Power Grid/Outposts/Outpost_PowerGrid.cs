﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Outposts;
using Verse;
using UnityEngine;

namespace VOEPowerGrid
{
    public class Outpost_PowerGrid : Outpost
    {
        [PostToSetings("VOEPowerGrid.Settings.PowerMultiplier", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.1f, 5f, null, null)]
        public float PowerMultiplier = 1f;

        public float currentStructureAmount => (ActiveBuildingsCounter.Count() > 0 ? ActiveBuildingsCounter.Sum((ThingDefCountClass tdcc) => tdcc.count * PowerGridExt.ConstructionOptions.FirstOrDefault((ConstructionOption co) => co.BuildingDef == tdcc.thingDef).ConstructionSkillsPerOne) : 0f);
        public float maxStructureAmount => VOEPowerGrid_Mod.Settings.BaseBuildingCapacity + TotalSkill(SkillDefOf.Construction) * VOEPowerGrid_Mod.Settings.BuildingCapacityPerSkill * terrainCapacityMultiplier;
        public float terrainCapacityMultiplier = 1f;
        public float terrainPowerMultiplier = 1f;

        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;

        private OutpostExtension_PowerGrid powerGridExt;
        public OutpostExtension_PowerGrid PowerGridExt => powerGridExt ?? (powerGridExt = def.GetModExtension<OutpostExtension_PowerGrid>());
        private ThingWithComps outlet;
        public ThingWithComps Outlet => outlet;
        public bool isNotConnected => outlet == null;
        private int TransmissionTowerAmount;
        public int PowerNetworkRange => TransmissionTowerAmount * VOEPowerGrid_Mod.Settings.TransmissionTowerRange;

        private bool isWorkingOnBuilding;
        public List<ThingDefCountClass> ConstructBuildingsCounter = new List<ThingDefCountClass>();
        public List<ThingDefCountClass> DeconstructBuildingsCounter = new List<ThingDefCountClass>();

        private float cashedProducedPower;
        public virtual float ProducedPower => cashedProducedPower;
        public List<ThingDefCountClass> BuildingsCounter => buildingsCounter ?? (buildingsCounter = new List<ThingDefCountClass>());
        public List<ThingDefCountClass> buildingsCounter;
        public List<ThingDefCountClass> ActiveBuildingsCounter => activeBuildingsCounter ?? (activeBuildingsCounter = new List<ThingDefCountClass>());
        public List<ThingDefCountClass> activeBuildingsCounter;

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            CalculateTerrainMultiplier();
            CheckStructureAmount();
        }

        public virtual void CalculateTerrainMultiplier()
        {
            terrainCapacityMultiplier = 1f;
            terrainPowerMultiplier = 1f;
            Tile tile = Find.WorldGrid[this.Tile];
            float mult = 1f;
            if (PowerGridExt.HillinessCapacity.TryGetValue(tile.hilliness, out mult))
            {
                terrainCapacityMultiplier *= mult;
            }
            float mult1 = 1f;
            if (PowerGridExt.HillinessPower.TryGetValue(tile.hilliness, out mult1))
            {
                terrainPowerMultiplier *= mult1;
            }
            SurfaceTile sTile = tile as SurfaceTile;
            if (!sTile.Rivers.NullOrEmpty())
            {
                float mult2 = 1f;
                if (!PowerGridExt.CanBuildOnWater)
                {
                    terrainCapacityMultiplier *= 0.75f;
                }
                else if (PowerGridExt.RiverCapacity.TryGetValue(sTile.Rivers.MaxBy((SurfaceTile.RiverLink r) => r.river.widthOnWorld).river, out mult2))
                {
                    terrainCapacityMultiplier *= mult2;
                }
            }
            float mult3 = 1f;
            if (PowerGridExt.BiomeCapacity.TryGetValue(tile.PrimaryBiome, out mult3))
            {
                terrainCapacityMultiplier *= mult3;
            }
            float mult4 = 1f;
            if (PowerGridExt.BiomePower.TryGetValue(tile.PrimaryBiome, out mult4))
            {
                terrainPowerMultiplier *= mult4;
            }
            if (PowerGridExt.CanBuildOnCoast)
            {
                List<PlanetTile> tmpNeighbors = new List<PlanetTile>();
                Find.WorldGrid.GetTileNeighbors(this.Tile, tmpNeighbors);
                int coasts = tmpNeighbors.Count(neighbor => Find.WorldGrid[neighbor].PrimaryBiome == BiomeDefOf.Ocean);
                terrainCapacityMultiplier *= Mathf.Pow(1.25f, Mathf.Max(0, coasts - 1));
            }
        }

        public ConstructionOption GetConstructionOption(ThingDef thingDef)
        {
            return PowerGridExt.ConstructionOptions.FirstOrDefault((ConstructionOption co) => co.BuildingDef == thingDef);
        }

        public void SetNewOutlet(ThingWithComps outletThing)
        {
            outlet = outletThing;
            if (outlet != null)
            {
                UpdateProducedPower();
            }
        }

        public void RecashProducedPower(float pp)
        {
            cashedProducedPower = pp;
        }

        public virtual void UpdateProducedPower()
        {
            RecashProducedPower(ActiveBuildingsCounter.Count() > 0 ? ActiveBuildingsCounter.Sum((ThingDefCountClass tdcc) => -tdcc.thingDef.GetCompProperties<CompProperties_Power>().PowerConsumption * tdcc.count) * terrainPowerMultiplier * PowerMultiplier : 0f);
            if (Outlet != null)
            {
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput();
            }
        }

        public override void RecachePawnTraits()
        {
            base.RecachePawnTraits();
            CheckStructureAmount();
        }

        public void CheckStructureAmount()
        {
            if (currentStructureAmount > maxStructureAmount)
            {
                while (currentStructureAmount > maxStructureAmount)
                {
                    ThingDefCountClass activeTDCC = ActiveBuildingsCounter.FirstOrDefault((ThingDefCountClass tdcc) => tdcc.count > 0);
                    if (activeTDCC != null)
                    {
                        activeTDCC.count--;
                    }
                    else
                    {
                        break;
                    }
                }
                UpdateProducedPower();
                Find.LetterStack.ReceiveLetter("VOEPowerGrid.Letters.ActiveBuildingReset.Label".Translate(Name), "VOEPowerGrid.Letters.ActiveBuildingReset.Text".Translate(), LetterDefOf.NeutralEvent);
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (isWorkingOnBuilding)
            {
                if (ConstructBuildingsCounter.Count() > 0)
                {
                    ThingDefCountClass tdcc = ConstructBuildingsCounter.FirstOrDefault();
                    tdcc.count--;
                    if (tdcc.count <= 0)
                    {
                        ConstructFinish(tdcc.thingDef);
                        ConstructBuildingsCounter.Remove(tdcc);
                    }
                }
                if (DeconstructBuildingsCounter.Count() > 0)
                {
                    ThingDefCountClass tdcc = DeconstructBuildingsCounter.FirstOrDefault();
                    tdcc.count--;
                    if (tdcc.count <= 0)
                    {
                        DeconstructFinish(tdcc.thingDef);
                        DeconstructBuildingsCounter.Remove(tdcc);
                    }
                }
                if (ConstructBuildingsCounter.NullOrEmpty() && DeconstructBuildingsCounter.NullOrEmpty())
                {
                    isWorkingOnBuilding = false;
                }
            }
        }

        public override void Produce()
        {
        }

        public void ConstructStart(ThingDef building)
        {
            if (CanConstruct(building))
            {
                ConstructBuildingsCounter.Add(new ThingDefCountClass(building, (int)building.GetStatValueAbstract(StatDefOf.WorkToBuild)));
                isWorkingOnBuilding = true;
                List<Thing> usedThings = new List<Thing>();
                if (HaveMinifiedVer(building))
                {
                    Thing miniThing = this.Things.FirstOrDefault((Thing t) => t is MinifiedThing ts ? ts.InnerThing.def == building : false);                    
                    usedThings.Add(TakeItem(miniThing));
                }
                else
                {
                    List<ThingDefCountClass> costToMake = BuildingCost(building);
                    foreach (ThingDefCountClass tdcc in costToMake)
                    {
                        usedThings.AddRange(TakeItems(tdcc.thingDef, tdcc.count));
                    }
                }
                foreach (Thing t in usedThings)
                    t.Destroy();
            }
            else
            {
                Log.Error("Not enough materials to build " + building.label);
            }
        }

        public void ConstructFinish(ThingDef building)
        {
            if (building == ThingDefOfLocal.PowerTransmissionTower)
            {
                TransmissionTowerAmount += 1;
            }
            else
            {
                int indexBC = BuildingsCounter.FirstIndexOf((ThingDefCountClass tdcc) => tdcc.thingDef == building);
                int indexABC = ActiveBuildingsCounter.FirstIndexOf((ThingDefCountClass tdcc) => tdcc.thingDef == building);
                if (indexBC >= 0)
                {
                    BuildingsCounter[indexBC].count++;
                    if (currentStructureAmount + PowerGridExt.ConstructionOptions.FirstOrDefault((ConstructionOption co) => co.BuildingDef == building).ConstructionSkillsPerOne <= maxStructureAmount)
                    {
                        ActiveBuildingsCounter[indexABC].count = Mathf.Min(BuildingsCounter[indexBC].count, ActiveBuildingsCounter[indexABC].count + 1); ;
                    }
                }
                else
                {
                    BuildingsCounter.Add(new ThingDefCountClass(building, 1));
                    ActiveBuildingsCounter.Add(new ThingDefCountClass(building, 1));
                }
                UpdateProducedPower();
            }
        }

        public bool HaveMinifiedVer(ThingDef building)
        {
            if (building.Minifiable && this.Things.Any((Thing t) => t is MinifiedThing ts ? ts.InnerThing.def == building : false))
                return true;
            return false;
        }

        public bool CanConstruct(ThingDef building)
        {
            bool isCostPaid = false;
            if (HaveMinifiedVer(building))
            {
                isCostPaid = true;
            }
            else if (building.BuildableByPlayer || building == ThingDefOfLocal.PowerTransmissionTower)
            {
                List<ThingDefCountClass> costToMake = BuildingCost(building);
                if (costToMake != null && costToMake.Count > 0)
                {
                    isCostPaid = true;
                    List<Thing> inventory = this.Things.Where((Thing t) => costToMake.Any((ThingDefCountClass tdcc) => tdcc.thingDef == t.def)).ToList();
                    foreach (Thing t in inventory)
                    {
                        costToMake.FirstOrDefault((ThingDefCountClass tdcc) => tdcc.thingDef == t.def).count -= t.stackCount;
                    }
                    foreach (ThingDefCountClass tdcc in costToMake)
                    {
                        if (tdcc.count > 0)
                            isCostPaid = false;
                    }
                }
            }
            return isCostPaid;
        }

        public void DeconstructStart(ThingDef building)
        {
            int indexBC = BuildingsCounter.FirstIndexOf((ThingDefCountClass tdcc) => tdcc.thingDef == building);
            int indexABC = ActiveBuildingsCounter.FirstIndexOf((ThingDefCountClass tdcc) => tdcc.thingDef == building);
            if (indexBC >= 0)
            {
                BuildingsCounter[indexBC].count--;
                ActiveBuildingsCounter[indexABC].count = Mathf.Min(BuildingsCounter[indexBC].count, ActiveBuildingsCounter[indexABC].count);
                if (BuildingsCounter[indexBC].count < 1)
                {
                    BuildingsCounter.RemoveAt(indexBC);
                    ActiveBuildingsCounter.RemoveAt(indexABC);
                }
                UpdateProducedPower();
                DeconstructBuildingsCounter.Add(new ThingDefCountClass(building, (int)Mathf.Clamp(building.GetStatValueAbstract(StatDefOf.WorkToBuild), 20f, 3000f)));
                isWorkingOnBuilding = true;
            }
            else if (building == ThingDefOfLocal.PowerTransmissionTower)
            {
                TransmissionTowerAmount -= 1;
                DeconstructBuildingsCounter.Add(new ThingDefCountClass(building, (int)Mathf.Clamp(building.GetStatValueAbstract(StatDefOf.WorkToBuild), 20f, 3000f)));
                isWorkingOnBuilding = true;
            }
            else
            {
                Log.Error("No building " + building.label + " exist");
                return;
            }
        }

        public void DeconstructFinish(ThingDef building)
        {
            if (building.Minifiable)
            {
                AddItem(MinifyUtility.MakeMinified(ThingMaker.MakeThing(building)));
            }
            else
            {
                List<ThingDefCountClass> deconstructionAmount = BuildingCost(building);
                foreach (ThingDefCountClass tdcc in deconstructionAmount)
                {
                    int dCount = Mathf.Min(GenMath.RoundRandom((float)tdcc.count * building.resourcesFractionWhenDeconstructed), tdcc.count);
                    if (dCount > 0)
                    {
                        IEnumerable<Thing> deconstructionThings = Utils.Make(tdcc.thingDef, dCount);
                        foreach (Thing t in deconstructionThings)
                            AddItem(t);
                    }
                }
            }
        }

        public List<ThingDefCountClass> BuildingCost(ThingDef building)
        {
            return (building.BuildableByPlayer || building == ThingDefOfLocal.PowerTransmissionTower) ? building.CostList.Select((ThingDefCountClass tdcc) => new ThingDefCountClass() { thingDef = tdcc.thingDef, count = tdcc.count }).ToList() : building.Minifiable ? new List<ThingDefCountClass>() { new ThingDefCountClass() { thingDef = building.minifiedDef, count = 1 } } : new List<ThingDefCountClass>();
        }

        public virtual float MaxPowerProducedByBuilding(ConstructionOption co)
        {
            return -co.BuildingDef.GetCompProperties<CompProperties_Power>().PowerConsumption * PowerMultiplier;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return new Command_Action
            {
                action = delegate
                {
                    List<FloatMenuOption> FMO = PowerGridExt.ConstructionOptions.Select(delegate (ConstructionOption co)
                    {
                        if (AllPawns.Where((Pawn p) => !p.Dead && p.RaceProps.Humanlike).Max((Pawn p) => p.skills.GetSkill(SkillDefOf.Construction).Level) < co.MinConstructionSkill)
                        {
                            return new FloatMenuOption("VOEPowerGrid.MaxSkillTooLow".Translate(co.MinConstructionSkill, AllPawns.Where((Pawn p) => !p.Dead && p.RaceProps.Humanlike).Max((Pawn p) => p.skills.GetSkill(SkillDefOf.Construction).Level).ToStringSafe()).RawText, action: null, shownItemForIcon: co.BuildingDef);
                        }
                        else if (!co.BuildingDef.IsResearchFinished)
                        {
                            return new FloatMenuOption("VOEPowerGrid.ResearchNotFinished".Translate().RawText, action: null, shownItemForIcon: co.BuildingDef);
                        }
                        else if (currentStructureAmount + co.ConstructionSkillsPerOne > maxStructureAmount)
                        {
                            return new FloatMenuOption("VOEPowerGrid.ConstructionSkillCostTooHigh".Translate(co.MinConstructionSkill).RawText, action: null, shownItemForIcon: co.BuildingDef);
                        }
                        else if (!CanConstruct(co.BuildingDef))
                        {
                            return new FloatMenuOption("VOEPowerGrid.ConstructionMaterialCostTooHigh".Translate(string.Join("\n", BuildingCost(co.BuildingDef).Select((ThingDefCountClass tdcc) => tdcc.Label))).RawText, action: null, shownItemForIcon: co.BuildingDef);
                        }
                        else
                        {
                            return new FloatMenuOption(co.BuildingDef.label + " [" + co.ConstructionSkillsPerOne.ToStringSafe() + "] +" + MaxPowerProducedByBuilding(co).ToStringSafe() + " :\n" + string.Join("\n", BuildingCost(co.BuildingDef).Select((ThingDefCountClass tdcc) => tdcc.Label)), delegate
                            {
                                ConstructStart(co.BuildingDef);
                            }, shownItemForIcon: co.BuildingDef);
                        }
                    })
                        .ToList();
                    if (!CanConstruct(ThingDefOfLocal.PowerTransmissionTower))
                    {
                        FMO.Add(new FloatMenuOption("VOEPowerGrid.ConstructionMaterialCostTooHigh".Translate(string.Join("\n", BuildingCost(ThingDefOfLocal.PowerTransmissionTower).Select((ThingDefCountClass tdcc) => tdcc.Label))).RawText, action: null, shownItemForIcon: ThingDefOfLocal.PowerTransmissionTower));
                    }
                    else
                    {
                        FMO.Add(new FloatMenuOption(ThingDefOfLocal.PowerTransmissionTower.label + " +" + VOEPowerGrid_Mod.Settings.TransmissionTowerRange.ToStringSafe() + " " + "VOEPowerGrid.PowerRange".Translate().RawText + " :\n" + string.Join("\n", BuildingCost(ThingDefOfLocal.PowerTransmissionTower).Select((ThingDefCountClass tdcc) => tdcc.Label)), delegate
                        {
                            ConstructStart(ThingDefOfLocal.PowerTransmissionTower);
                        }, shownItemForIcon: ThingDefOfLocal.PowerTransmissionTower));
                    }
                    Find.WindowStack.Add(new FloatMenu(FMO));
                },
                defaultLabel = "VOEPowerGrid.Construct.Label".Translate().RawText,
                defaultDesc = "VOEPowerGrid.Construct.Desc".Translate().RawText,
                icon = ContentFinder<Texture2D>.Get("UI/Designators/HomeAreaOn")
            };
            yield return new Command_Action
            {
                action = delegate
                {
                    List<FloatMenuOption> FMO = BuildingsCounter.Select(delegate (ThingDefCountClass tdcc)
                    {
                        return new FloatMenuOption(tdcc.count.ToStringSafe() + " " + tdcc.thingDef.label.ToStringSafe(), delegate
                        {
                            DeconstructStart(tdcc.thingDef);
                        }, shownItemForIcon: tdcc.thingDef);
                    })
                        .ToList();
                    if (TransmissionTowerAmount > 0)
                    {
                        if (Outlet != null && PowerNetworkRange <= Find.WorldGrid.TraversalDistanceBetween(Tile, Outlet.Tile))
                        {
                            FMO.Add(new FloatMenuOption("VOEPowerGrid.AllPowerTransmissionTowerUsed".Translate().RawText, action: null, shownItemForIcon: ThingDefOfLocal.PowerTransmissionTower));
                        }
                        else
                        {
                            FMO.Add(new FloatMenuOption(((PowerNetworkRange - (Outlet != null ? Find.WorldGrid.TraversalDistanceBetween(Tile, Outlet.Tile) : 0)) / VOEPowerGrid_Mod.Settings.TransmissionTowerRange).ToStringSafe() + " " + ThingDefOfLocal.PowerTransmissionTower.label, delegate
                            {
                                DeconstructStart(ThingDefOfLocal.PowerTransmissionTower);
                            }, shownItemForIcon: ThingDefOfLocal.PowerTransmissionTower));
                        }
                    }
                    Find.WindowStack.Add(new FloatMenu(FMO));
                },
                defaultLabel = "VOEPowerGrid.Deconstruct.Label".Translate().RawText,
                defaultDesc = "VOEPowerGrid.Deconstruct.Desc".Translate().RawText,
                icon = ContentFinder<Texture2D>.Get("UI/Designators/HomeAreaOff"),
                Disabled = BuildingsCounter.NullOrEmpty() && TransmissionTowerAmount <= 0,
                disabledReason = "VOEPowerGrid.Deconstruct.Reason".Translate().RawText
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref buildingsCounter, "BuildingsCounter", LookMode.Deep);
            Scribe_Collections.Look(ref activeBuildingsCounter, "activeBuildingsCounter", LookMode.Deep);
            Scribe_Collections.Look(ref ConstructBuildingsCounter, "ConstructBuildingsCounter", LookMode.Deep);
            Scribe_Collections.Look(ref DeconstructBuildingsCounter, "DeconstructBuildingsCounter", LookMode.Deep);
            Scribe_Values.Look(ref isWorkingOnBuilding, "isWorkingOnBuilding");
            Scribe_Values.Look(ref cashedProducedPower, "cashedProducedPower");
            Scribe_Values.Look(ref TransmissionTowerAmount, "TransmissionTowerAmount");
            Scribe_References.Look(ref outlet, "outlet");
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            GenDraw.DrawWorldRadiusRing(this.Tile, PowerNetworkRange);
        }

        public override void Destroy()
        {
            RecashProducedPower(0f);
            if (Outlet != null)
            {
                Outlet.GetComp<CompPowerGridOutlet>().Disconnect();
            }
            base.Destroy();
        }

        public override string ProductionString()
        {
            if (Ext == null)
            {
                return "";
            }
            List<string> inspectStrings = new List<string>();
            inspectStrings.Add("VOEPowerGrid.Base.ProductionString".Translate(isNotConnected ? "VOEPowerGrid.Outlet.NotConnected".Translate().RawText : "VOEPowerGrid.Outlet.Connected".Translate(outlet.Map.Parent.Label).RawText, currentStructureAmount.ToStringSafe(), maxStructureAmount.ToStringSafe(), PowerNetworkRange.ToStringSafe(), TransmissionTowerAmount.ToStringSafe(), ProducedPower.ToStringSafe()).RawText);
            if (ConstructBuildingsCounter.Count() > 0)
            {
                inspectStrings.Add("VOEPowerGrid.Base.ConstructionString".Translate(string.Join("\n", ConstructBuildingsCounter.Select((ThingDefCountClass tdcc) => tdcc.thingDef.label + " " + tdcc.count.ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor)))).RawText);
            }
            if (DeconstructBuildingsCounter.Count() > 0)
            {
                inspectStrings.Add("VOEPowerGrid.Base.DeconstructionString".Translate(string.Join("\n", DeconstructBuildingsCounter.Select((ThingDefCountClass tdcc) => tdcc.thingDef.label + " " + tdcc.count.ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor)))).RawText);
            }
            if (terrainCapacityMultiplier != 1f)
            {
                inspectStrings.Add("VOEPowerGrid.Base.TerrainCapacityString".Translate(terrainCapacityMultiplier.ToString("F")));
            }
            if (terrainPowerMultiplier != 1f)
            {
                inspectStrings.Add("VOEPowerGrid.Base.TerrainPowerString".Translate(terrainPowerMultiplier.ToString("F")));
            }
            return string.Join("\n", inspectStrings);
        }
    }
}
