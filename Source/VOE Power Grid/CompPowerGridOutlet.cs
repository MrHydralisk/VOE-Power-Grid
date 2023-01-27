using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace VOEPowerGrid
{
    [StaticConstructorOnStartup]
    public class CompPowerGridOutlet : CompPowerPlant
    {
        private Outpost_PowerGrid outpostPowerGrid;
        public Outpost_PowerGrid OutpostPowerGrid => outpostPowerGrid;
        private float powerLossDueToRange;
        protected override float DesiredPowerOutput => outpostPowerGrid?.ProducedPower ?? 0f;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            outpostPowerGrid?.SetNewOutlet(null);
            base.PostDestroy(mode, previousMap);
        }

        public override void SetUpPowerVars()
        {
            base.SetUpPowerVars();
#if v1_3
               UpdateDesiredPowerOutput1_3();
#elif v1_4
            UpdateDesiredPowerOutput();
#endif
        }

        public void Disconnect()
        {
            outpostPowerGrid?.SetNewOutlet(null);
            outpostPowerGrid = null;
            powerLossDueToRange = 0;
#if v1_3
            UpdateDesiredPowerOutput1_3();
#elif v1_4
            UpdateDesiredPowerOutput();
#endif
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            List<Outpost_PowerGrid> powerGrids = Find.WorldObjects.AllWorldObjects.OfType<Outpost_PowerGrid>().OrderBy((Outpost_PowerGrid opd) => Find.WorldGrid.ApproxDistanceInTiles(opd.Tile, this.parent.Tile)).ToList();
            yield return new Command_Action
            {
                action = delegate
                {
                    List<FloatMenuOption> FMO = powerGrids.Select(delegate (Outpost_PowerGrid opd)
                    {
                        if (!opd.isNotConnected)
                        {
                            return new FloatMenuOption("VOEPowerGrid.Outlet.AlreadyConnected".Translate(opd.Label).RawText, action: null, itemIcon: opd.ExpandingIcon, iconColor: opd.ExpandingIconColor);
                        }
                        else if (opd.PowerNetworkRange < Find.WorldGrid.TraversalDistanceBetween(this.parent.Tile, opd.Tile))
                        {
                            return new FloatMenuOption("VOEPowerGrid.Outlet.PowerRangeNotEnough".Translate(opd.Label, opd.PowerNetworkRange, Find.WorldGrid.TraversalDistanceBetween(this.parent.Tile, opd.Tile)).RawText, action: null, itemIcon: opd.ExpandingIcon, iconColor: opd.ExpandingIconColor);
                        }
                        else
                        {
                            return new FloatMenuOption(opd.Label + " +" + opd.ProducedPower.ToStringSafe(), delegate
                            {
                                outpostPowerGrid?.SetNewOutlet(null);
                                outpostPowerGrid = opd;
                                powerLossDueToRange = VOEPowerGrid_Mod.Settings.PowerLossPerTiles > 0 ? Mathf.Min(1f, Mathf.Max(0f, ((Find.WorldGrid.TraversalDistanceBetween(this.parent.Tile, opd.Tile) / (float)VOEPowerGrid_Mod.Settings.PowerLossPerTiles) - 1f) / 100)) : 0f;
                                outpostPowerGrid?.SetNewOutlet(this.parent);
                            }, itemIcon: opd.ExpandingIcon, iconColor: opd.ExpandingIconColor);
                        }                    
                    })
                        .ToList();
                    if (outpostPowerGrid != null)
                        FMO.Add(new FloatMenuOption("VOEPowerGrid.Outlet.Disconnect".Translate(outpostPowerGrid.Label).RawText, delegate
                        {
                            Disconnect();
                        }, itemIcon: outpostPowerGrid.ExpandingIcon, iconColor: outpostPowerGrid.ExpandingIconColor));
                    Find.WindowStack.Add(new FloatMenu(FMO));
                },
                defaultLabel = "VOEPowerGrid.Outlet.Connect.Label".Translate().RawText,
                defaultDesc = "VOEPowerGrid.Outlet.Connect.Desc".Translate().RawText,
                icon = TexOutposts.ConnectTex,
                disabled = powerGrids.Count <= 0,
                disabledReason = "VOEPowerGrid.Outlet.Connect.Reason".Translate().RawText
            };
        }
#if v1_3
        public virtual void UpdateDesiredPowerOutput1_3()
#elif v1_4
        public override void UpdateDesiredPowerOutput()
#endif
        {
            PowerOutput = DesiredPowerOutput * (1f - powerLossDueToRange);
        }

        public override string CompInspectStringExtra()
        {
            string s = "";
            if (outpostPowerGrid != null)
                s += "VOEPowerGrid.Outlet.Connected".Translate(outpostPowerGrid.Label).RawText + "VOEPowerGrid.Outlet.PowerLoss".Translate(powerLossDueToRange.ToStringPercent()).RawText;
            else
                s += "VOEPowerGrid.Outlet.NotConnected".Translate().RawText;
            return s + base.CompInspectStringExtra();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref outpostPowerGrid, "outpostPowerGrid");
            Scribe_Values.Look(ref powerLossDueToRange, "powerLossDueToRange");
        }
    }
}
