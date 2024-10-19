using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace SafeTemperature
{
    public class SafeTemperatureSettings : ModSettings
    {
        public static bool SeekSafeTemperatureIntervalEnabled = true;
        public static int SeekSafeTemperatureIntervalTicks = 60;
        public static bool UseSuboptimalTemperatureRegions = true;
        public static FloatRange HypothermiaSeverityRange = new FloatRange(0.04f, 0.35f);
        public static FloatRange HeatstrokeSeverityRange = new FloatRange(0.04f, 0.35f);

        public static void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("SafeTemperature_SeekSafeTemperatureIntervalEnabled".Translate(), ref SeekSafeTemperatureIntervalEnabled, "SafeTemperature_SeekSafeTemperatureIntervalEnabledDesc".Translate());
            if (SeekSafeTemperatureIntervalEnabled)
            {
                listingStandard.Indent();
                SeekSafeTemperatureIntervalTicks = (int)listingStandard.SliderLabeled("SafeTemperature_SeekSafeTemperatureIntervalTicks".Translate(SeekSafeTemperatureIntervalTicks), SeekSafeTemperatureIntervalTicks, 1, 600, 0.5f, "SafeTemperature_SeekSafeTemperatureIntervalTicksDesc".Translate());
                listingStandard.Outdent();
            }
            listingStandard.CheckboxLabeled("SafeTemperature_UseSuboptimalTemperatureRegions".Translate(), ref UseSuboptimalTemperatureRegions, "SafeTemperature_UseSuboptimalTemperatureRegionsDesc".Translate());
            listingStandard.End();

            float y = listingStandard.CurHeight;
            float rangeHeight = 32f;
            Rect hypothermiaRect = new Rect(inRect.x, inRect.y + y, inRect.width - 200f, rangeHeight);
            Widgets.FloatRange(hypothermiaRect, HypothermiaSeverityRange.GetHashCode(), ref HypothermiaSeverityRange, 0f, 1f, "SafeTemperature_HypothermiaSeverityRange", ToStringStyle.FloatTwo, 0f, GameFont.Small, Color.white);
            TooltipHandler.TipRegionByKey(hypothermiaRect, "SafeTemperature_HypothermiaSeverityRangeDesc");
            if (Widgets.ButtonText(new Rect(inRect.xMax - 200f, inRect.y + y, 100f, rangeHeight), "SafeTemperature_SetMinTo".Translate()))
            {
                DoSeverityFloatMenu(HediffDefOf.Hypothermia, x => HypothermiaSeverityRange.min = x);
            }
            if (Widgets.ButtonText(new Rect(inRect.xMax - 100f, inRect.y + y, 100f, rangeHeight), "SafeTemperature_SetMaxTo".Translate()))
            {
                DoSeverityFloatMenu(HediffDefOf.Hypothermia, x => HypothermiaSeverityRange.max = x);
            }
            y += rangeHeight;
            Rect heatstrokeRect = new Rect(inRect.x, inRect.y + y, inRect.width - 200f, rangeHeight);
            Widgets.FloatRange(heatstrokeRect, HeatstrokeSeverityRange.GetHashCode(), ref HeatstrokeSeverityRange, 0f, 1f, "SafeTemperature_HeatstrokeSeverityRange", ToStringStyle.FloatTwo, 0f, GameFont.Small, Color.white);
            TooltipHandler.TipRegionByKey(heatstrokeRect, "SafeTemperature_HeatstrokeSeverityRangeDesc");
            if (Widgets.ButtonText(new Rect(inRect.xMax - 200f, inRect.y + y, 100f, rangeHeight), "SafeTemperature_SetMinTo".Translate()))
            {
                DoSeverityFloatMenu(HediffDefOf.Heatstroke, x => HeatstrokeSeverityRange.min = x);
            }
            if (Widgets.ButtonText(new Rect(inRect.xMax - 100f, inRect.y + y, 100f, rangeHeight), "SafeTemperature_SetMaxTo".Translate()))
            {
                DoSeverityFloatMenu(HediffDefOf.Heatstroke, x => HeatstrokeSeverityRange.max = x);
            }
            y += rangeHeight;

            HypothermiaSeverityRange.min = Mathf.Min(HypothermiaSeverityRange.min, HypothermiaSeverityRange.max);
            HypothermiaSeverityRange.max = Mathf.Max(HypothermiaSeverityRange.min, HypothermiaSeverityRange.max);
            HeatstrokeSeverityRange.min = Mathf.Min(HeatstrokeSeverityRange.min, HeatstrokeSeverityRange.max);
            HeatstrokeSeverityRange.max = Mathf.Max(HeatstrokeSeverityRange.min, HeatstrokeSeverityRange.max);
        }

        private static void DoSeverityFloatMenu(HediffDef def, Action<float> callback)
        {
            Find.WindowStack.Add(new FloatMenu(def.stages.Select(s => new FloatMenuOption(s.label + " (" + s.minSeverity * 100 + "%)", delegate
            {
                callback(s.minSeverity);
            })).ToList()));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref SeekSafeTemperatureIntervalEnabled, "SeekSafeTemperatureIntervalEnabled", true);
            Scribe_Values.Look(ref SeekSafeTemperatureIntervalTicks, "SeekSafeTemperatureIntervalTicks", 60);
            Scribe_Values.Look(ref UseSuboptimalTemperatureRegions, "UseSuboptimalTemperatureRegions", true);
            Scribe_Values.Look(ref HypothermiaSeverityRange, "HypothermiaSeverityRange", new FloatRange(0.04f, 0.35f));
            Scribe_Values.Look(ref HypothermiaSeverityRange, "HeatstrokeSeverityRange", new FloatRange(0.04f, 0.35f));
        }
    }
}
