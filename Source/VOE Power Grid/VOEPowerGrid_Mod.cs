using UnityEngine;
using RimWorld;
using Verse;

namespace VOEPowerGrid
{
    public class VOEPowerGrid_Mod : Mod
    {
        public static VOEPowerGrid_Settings Settings { get; private set; }

        public VOEPowerGrid_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<VOEPowerGrid_Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard options = new Listing_Standard();
            options.Begin(inRect);
            options.Label("VOEPowerGrid.Settings.BuildingCapacityPerSkill".Translate(Settings.BuildingCapacityPerSkill.ToString()));
            Settings.BuildingCapacityPerSkill = Mathf.Round(options.Slider(Settings.BuildingCapacityPerSkill, 0.1f, 5f)*100)/100;
            options.Label("VOEPowerGrid.Settings.BaseBuildingCapacity".Translate(Settings.BaseBuildingCapacity.ToString()));
            Settings.BaseBuildingCapacity = (int)options.Slider(Settings.BaseBuildingCapacity, 0, 20);
            options.Label("VOEPowerGrid.Settings.PowerLossPerTiles".Translate(Settings.PowerLossPerTiles.ToString()));
            Settings.PowerLossPerTiles = (int)options.Slider(Settings.PowerLossPerTiles, 0, 10);
            options.Label("VOEPowerGrid.Settings.TransmissionTowerRange".Translate(Settings.TransmissionTowerRange.ToString()));
            Settings.TransmissionTowerRange = (int)options.Slider(Settings.TransmissionTowerRange, 1, 5);
            options.End();
        }

        public override string SettingsCategory()
        {
            return "VOEPowerGrid.Settings.Title".Translate().RawText;
        }
    }
}
