using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VOEPowerGrid
{
    [StaticConstructorOnStartup]
    public class CompPowerGridOutlet : CompPowerPlant
    {
        private Outpost_PowerGrid outpostPowerGrid;
        public Outpost_PowerGrid OutpostPowerGrid => outpostPowerGrid;
        protected override float DesiredPowerOutput => outpostPowerGrid?.ProducedPower ?? 0f;
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            outpostPowerGrid?.SetNewOutlet(null);
            base.PostDestroy(mode, previousMap);
        }

        public void Disconnect()
        {
            outpostPowerGrid?.SetNewOutlet(null);
            outpostPowerGrid = null;
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
                        return opd.isNotConnected ? new FloatMenuOption(opd.Label + " +" + opd.ProducedPower.ToStringSafe(), delegate
                        {
                            outpostPowerGrid?.SetNewOutlet(null);
                            outpostPowerGrid = opd;
                            outpostPowerGrid?.SetNewOutlet(this.parent);
                        }, itemIcon: opd.ExpandingIcon, iconColor: opd.ExpandingIconColor) : new FloatMenuOption("VOEPowerGrid.Outlet.AlreadyConnected".Translate(opd.Label).RawText, action: null, itemIcon: opd.ExpandingIcon, iconColor: opd.ExpandingIconColor);
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
            PowerOutput = DesiredPowerOutput;
        }

        public override string CompInspectStringExtra()
        {
            string s = "";
            if (outpostPowerGrid != null)
                s += "VOEPowerGrid.Outlet.Connected".Translate(outpostPowerGrid.Label).RawText;
            else
                s += "VOEPowerGrid.Outlet.NotConnected".Translate().RawText;
            return s + base.CompInspectStringExtra();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref outpostPowerGrid, "outpostPowerGrid");
        }
    }
}
