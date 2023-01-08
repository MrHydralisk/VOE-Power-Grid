using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Outposts;
using Verse;
using UnityEngine;

namespace VOEPowerGrid
{
    public class Outpost_PowerGrid : Outpost
    {
        [PostToSetings("VOEPowerGrid.Settings.PowerMultiplier", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.1f, 5f, null, null)]
        public float PowerMultiplier = 1f;

        [PostToSetings("VOEPowerGrid.Settings.BuildingCapacityPerSkill", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.1f, 5f, null, null)]
        public float BuildingCapacityPerSkill = 1f;

        [PostToSetings("VOEPowerGrid.Settings.BaseBuildingCapacity", PostToSetingsAttribute.DrawMode.IntSlider, 0, 0, 20, null, null)]
        public int BaseBuildingCapacity = 0;

        private float currentStructureAmount => BuildingsCounter.Count() > 0 ? BuildingsCounter.Sum((ThingDefCountClass tdcc) => tdcc.count * PowerGridExt.ConstructionOptions.FirstOrDefault((ConstructionOption co) => co.BuildingDef == tdcc.thingDef).ConstructionSkillsPerOne) : 0f;
        public float maxStructureAmount => BaseBuildingCapacity + TotalSkill(SkillDefOf.Construction) * BuildingCapacityPerSkill;

        protected OutpostExtension_Choose ChooseExt => base.Ext as OutpostExtension_Choose;

        private OutpostExtension_PowerGrid powerGridExt;
        public OutpostExtension_PowerGrid PowerGridExt => powerGridExt ?? (powerGridExt = def.GetModExtension<OutpostExtension_PowerGrid>());
        private ThingWithComps outlet;
        public ThingWithComps Outlet => outlet;
        public bool isNotConnected => outlet == null;

        private float cashedProducedPower;
        public virtual float ProducedPower => cashedProducedPower;
        public List<ThingDefCountClass> BuildingsCounter = new List<ThingDefCountClass>();

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
        public void recashProducedPower(float pp)
        {
            cashedProducedPower = pp;
        }
        public virtual void UpdateProducedPower()
        {
            recashProducedPower(BuildingsCounter.Count() > 0 ? BuildingsCounter.Sum((ThingDefCountClass tdcc) => -tdcc.thingDef.GetCompProperties<CompProperties_Power>().
#if v1_3
            basePowerConsumption
#elif v1_4
            PowerConsumption
#endif
            * tdcc.count) * PowerMultiplier : 0f);
            if (Outlet != null)
            {
#if v1_3
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput1_3();
#elif v1_4
                Outlet.GetComp<CompPowerGridOutlet>().UpdateDesiredPowerOutput();
#endif
            }
        }
        public override void Produce()
        {
        }
        public void Construct(ThingDef building)
        {
            if (CanConstruct(building))
            {
                int indexBC = BuildingsCounter.FirstIndexOf((ThingDefCountClass tdcc) => tdcc.thingDef == building);
                if (indexBC >= 0)
                {
                    BuildingsCounter[indexBC].count++;
                }
                else
                {
                    BuildingsCounter.Add(new ThingDefCountClass(building, 1));
                }
                UpdateProducedPower();
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
            else if (building.BuildableByPlayer)
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

        public void Deconstruct(ThingDef building)
        {
            int indexBC = BuildingsCounter.FirstIndexOf((ThingDefCountClass tdcc) => tdcc.thingDef == building);
            if (indexBC >= 0)
            {
                BuildingsCounter[indexBC].count--;
                if (BuildingsCounter[indexBC].count < 1)
                    BuildingsCounter.RemoveAt(indexBC);
                UpdateProducedPower();
                if (building.Minifiable)
                {
                    AddItem(MinifyUtility.MakeMinified(ThingMaker.MakeThing(building)));
                }
                else
                {
                    List<ThingDefCountClass> deconstructionAmount = BuildingCost(building);
                    foreach (ThingDefCountClass tdcc in deconstructionAmount)
                    {
                        IEnumerable<Thing> deconstructionThings = Utils.Make(tdcc.thingDef, Mathf.Min(GenMath.RoundRandom((float)tdcc.count * building.resourcesFractionWhenDeconstructed), tdcc.count));
                        foreach (Thing t in deconstructionThings)
                            AddItem(t);
                    }
                }
            }
            else
            {
                Log.Error("No building " + building.label + " exist");
            }
        }

        public List<ThingDefCountClass> BuildingCost(ThingDef building)
        {
            return building.BuildableByPlayer ? building.CostList.Select((ThingDefCountClass tdcc) => new ThingDefCountClass() { thingDef = tdcc.thingDef, count = tdcc.count }).ToList() : building.Minifiable ? new List<ThingDefCountClass>() { new ThingDefCountClass() { thingDef = building.minifiedDef, count = 1 } } : new List<ThingDefCountClass>();
        }

        public virtual float MaxPowerProducedByBuilding(ConstructionOption co)
        {
            return -co.BuildingDef.GetCompProperties<CompProperties_Power>().
#if v1_3
                basePowerConsumption
#elif v1_4
                PowerConsumption
#endif 
                * PowerMultiplier;
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
                    Find.WindowStack.Add(new FloatMenu(PowerGridExt.ConstructionOptions.Select(delegate (ConstructionOption co)
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
                                Construct(co.BuildingDef);
                            }, shownItemForIcon: co.BuildingDef);
                        }
                    })
                        .ToList()));
                },
                defaultLabel = "VOEPowerGrid.Construct.Label".Translate().RawText,
                defaultDesc = "VOEPowerGrid.Construct.Desc".Translate().RawText,
                icon = ContentFinder<Texture2D>.Get("UI/Designators/HomeAreaOn"),
                disabled = currentStructureAmount >= maxStructureAmount,
                disabledReason = "VOEPowerGrid.Construct.Reason".Translate().RawText
            };
            yield return new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new FloatMenu(BuildingsCounter.Select(delegate (ThingDefCountClass tdcc)
                    {
                        return new FloatMenuOption(tdcc.count.ToStringSafe() + " " + tdcc.thingDef.label.ToStringSafe(), delegate
                        {
                            Deconstruct(tdcc.thingDef);
                        }, shownItemForIcon: tdcc.thingDef);
                    })
                        .ToList()));
                },
                defaultLabel = "VOEPowerGrid.Deconstruct.Label".Translate().RawText,
                defaultDesc = "VOEPowerGrid.Deconstruct.Desc".Translate().RawText,
                icon = ContentFinder<Texture2D>.Get("UI/Designators/HomeAreaOff"),
                disabled = BuildingsCounter.NullOrEmpty(),
                disabledReason = "VOEPowerGrid.Deconstruct.Reason".Translate().RawText
            };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref BuildingsCounter, "BuildingsCounter", LookMode.Deep);
            Scribe_Values.Look(ref cashedProducedPower, "cashedProducedPower");
            Scribe_References.Look(ref outlet, "outlet");
        }

        public override void Destroy()
        {
            recashProducedPower(0f);
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
            return "VOEPowerGrid.Base.ProductionString".Translate(isNotConnected ? "VOEPowerGrid.Outlet.NotConnected".Translate().RawText : "VOEPowerGrid.Outlet.Connected".Translate(outlet.Map.Parent.Label).RawText, currentStructureAmount.ToStringSafe(), maxStructureAmount.ToStringSafe(), ProducedPower.ToStringSafe()).RawText;
        }
    }
}
