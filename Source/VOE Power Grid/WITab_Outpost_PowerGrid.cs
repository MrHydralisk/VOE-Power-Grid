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
            Rect outRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            //Widgets.DrawBoxSolid(outRect, Color.black);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollViewHeight);
            //Widgets.DrawBoxSolid(viewRect, Color.gray);
            //Rect rect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(scrollViewHeight, outRect.height));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float curY = 0f;
            DoRows(ref curY, viewRect);
            //DoRows(ref curY, rect);
            scrollViewHeight = curY;
            Widgets.EndScrollView();
        }

        private void DoRows(ref float curY, Rect scrollOutRect)
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
            Rect rect = new Rect(0f, curY, width, 28f);
            //Widgets.DrawBoxSolid(rect, Color.white);
            Widgets.InfoCardButton(rect.width - 24f, curY, thingDCC.thingDef);
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
