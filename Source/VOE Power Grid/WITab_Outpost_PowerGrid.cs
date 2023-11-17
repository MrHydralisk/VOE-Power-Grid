using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VOEPowerGrid
{
    [StaticConstructorOnStartup]
    public class WITab_Outpost_PowerGrid : WITab
    {
        private Vector2 scrollPosition;
        private float scrollViewHeight;
        public Outpost_PowerGrid SelPowerGrid => base.SelObject as Outpost_PowerGrid;

        public WITab_Outpost_PowerGrid()
        {
            size = new Vector2(500f, 500f);
            labelKey = "VOEPowerGrid.PowerGridTab";
        }

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect outRect = new Rect(0f, 0f, size.x, size.y);
            float curY = 0f;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(0, curY, outRect.width - 114, 24f), " " + "VOEPowerGrid.Base.AvailableBuildingString".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(outRect.width - 114, curY, 76, 24f), SelPowerGrid.currentStructureAmount.ToStringSafe() + " / " + SelPowerGrid.maxStructureAmount.ToStringSafe());
            curY += 24f;
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(0f, curY, size.x);
            curY += 2f;
            outRect.yMin = curY;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            curY = 0f;
            DoRows(viewRect, ref curY);
            scrollViewHeight = curY;
            Widgets.EndScrollView();
        }

        private void DoRows(Rect scrollOutRect, ref float curY)
        {
            List<ThingDefCountClass> buildings = SelPowerGrid.BuildingsCounter;
            Widgets.BeginGroup(scrollOutRect);
            bool flag = false;
            for (int i = 0; i < buildings.Count; i++)
            {
                ThingDefCountClass t = buildings[i];
                if (t != null)
                {
                    flag = true;
                    DoThingRow(t, scrollOutRect.width, ref curY);
                }
            }
            if (!flag)
            {
                Widgets.NoneLabel(ref curY, scrollOutRect.width);
            }
            Widgets.EndGroup();
        }

        protected virtual void DoThingRow(ThingDefCountClass thingDCC, float width, ref float curY)
        {
            ThingDefCountClass activeBuilding = SelPowerGrid.ActiveBuildingsCounter.FirstOrDefault((ThingDefCountClass tdcc) => tdcc.thingDef == thingDCC.thingDef);
            if (activeBuilding == null)
            {
                SelPowerGrid.ActiveBuildingsCounter.Add(new ThingDefCountClass(thingDCC.thingDef, 0));
                activeBuilding = SelPowerGrid.ActiveBuildingsCounter.FirstOrDefault((ThingDefCountClass tdcc) => tdcc.thingDef == thingDCC.thingDef);
            }
            Rect rect = new Rect(0f, curY, width, 28f);
            Widgets.InfoCardButton(rect.width - 24f, curY, thingDCC.thingDef);
            rect.width -= 24f;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - 24f, rect.y + (rect.height - 24f) / 2f, 24f, 24f), "+"))
            {
                if (SelPowerGrid.currentStructureAmount + SelPowerGrid.PowerGridExt.ConstructionOptions.FirstOrDefault((ConstructionOption co) => co.BuildingDef == thingDCC.thingDef).ConstructionSkillsPerOne <= SelPowerGrid.maxStructureAmount)
                {
                    activeBuilding.count = Mathf.Min(thingDCC.count, activeBuilding.count + 1);
                    SelPowerGrid.UpdateProducedPower();
                }
            }
            rect.width -= 24f;
            Widgets.Label(new Rect(rect.width - 24f, curY, 24f, rect.height), activeBuilding.count.ToStringSafe());
            rect.width -= 24f;
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - 24f, rect.y + (rect.height - 24f) / 2f, 24f, 24f), "-"))
            {
                activeBuilding.count = Mathf.Max(0, activeBuilding.count - 1);
                SelPowerGrid.UpdateProducedPower();
            }
            rect.width -= 24f;
            if (Mouse.IsOver(rect))
            {
                GUI.color = ITab_Pawn_Gear.HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (thingDCC.thingDef.DrawMatSingle != null && thingDCC.thingDef.DrawMatSingle.mainTexture != null)
            {
                Rect rect2 = new Rect(4f, curY, 28f, 28f);
                Widgets.ThingIcon(rect2, thingDCC.thingDef);
            }
            Rect rect3 = new Rect(36f, curY, rect.width - 36f, rect.height);
            GUI.color = ITab_Pawn_Gear.ThingLabelColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            Widgets.Label(rect3, thingDCC.LabelCap.StripTags().Truncate(rect3.width));
            Text.WordWrap = true;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, thingDCC.LabelCap);
            curY += 28f;
        }
    }
}
