using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace SafeTemperature
{
    public static class TemperatureUtility
    {
        public static bool HasMinTemperatureInjury(this Pawn pawn)
        {
            Hediff hypothermia = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
            if (hypothermia != null && hypothermia.Severity >= SafeTemperatureSettings.HypothermiaSeverityRange.min)
            {
                return true;
            }
            Hediff heatstroke = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
            if (heatstroke != null && heatstroke.Severity >= SafeTemperatureSettings.HeatstrokeSeverityRange.min)
            {
                return true;
            }
            return false;
        }

        public static bool HasMaxTemperatureInjury(this Pawn pawn)
        {
            Hediff hypothermia = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
            if (hypothermia != null && hypothermia.Severity >= SafeTemperatureSettings.HypothermiaSeverityRange.max)
            {
                return true;
            }
            Hediff heatstroke = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
            if (heatstroke != null && heatstroke.Severity >= SafeTemperatureSettings.HeatstrokeSeverityRange.max)
            {
                return true;
            }
            return false;
        }

        public static Region ClosestRegionToTemperatureRange(IntVec3 root, Map map, Pawn pawn, FloatRange tempRange, TraverseParms traverseParms, bool above)
        {
            Region rootRegion = root.GetRegion(map);
            if (rootRegion == null)
            {
                return null;
            }

            IEnumerable<Region> consideredRegions = RegionsToConsider(rootRegion, traverseParms, map, pawn);

            float coldestTemp = float.MaxValue;
            float warmestTemp = float.MinValue;
            float? lastTemp = null;
            bool differentTemps = false;
            foreach (Region region in consideredRegions)
            {
                float temp = region.Room.Temperature;
                if (lastTemp != null && temp != lastTemp)
                {
                    differentTemps = true;
                }
                lastTemp = temp;
                if (above && temp < coldestTemp && temp > tempRange.max)
                {
                    coldestTemp = temp;
                }
                if (!above && temp > warmestTemp && temp < tempRange.min)
                {
                    warmestTemp = temp;
                }
            }

            if (!differentTemps)
            {
                return null;
            }
            
            return ClosestRegionInTemperature(rootRegion, consideredRegions.ToList(), above ? coldestTemp : warmestTemp, traverseParms, pawn);
        }

        public static bool IsValidRegionForPawn(Region region, Pawn pawn)
        {
            return !region.IsDoorway && JobGiver_SeekSafeTemperature.TryGetAllowedCellInRegion(region, pawn, out IntVec3 intVec) && pawn.CanReach(new LocalTargetInfo(intVec), PathEndMode.OnCell, Danger.Deadly);
        }

        private static IEnumerable<Region> RegionsToConsider(Region rootRegion, TraverseParms traverseParms, Map map, Pawn pawn)
        {
            Region closestOutdoorRegion = ClosestOutdoorRegion(rootRegion, traverseParms, pawn);
            return map.regionGrid.AllRegions.Where(r => (!r.Room.UsesOutdoorTemperature || r == closestOutdoorRegion) && IsValidRegionForPawn(r, pawn));
        }

        private static Region ClosestRegionInTemperature(Region rootRegion, List<Region> acceptableRegions, float temperature, TraverseParms traverseParms, Pawn pawn)
        {
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (acceptableRegions.Contains(r) && r.Room.Temperature == temperature)
                {
                    foundReg = r;
                    return true;
                }
                return false;
            };
            RegionTraverser.BreadthFirstTraverse(rootRegion, entryCondition, regionProcessor);
            return foundReg;
        }

        private static Region ClosestOutdoorRegion(Region rootRegion, TraverseParms traverseParms, Pawn pawn)
        {
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (IsValidRegionForPawn(r, pawn) && r.Room.UsesOutdoorTemperature)
                {
                    foundReg = r;
                    return true;
                }
                return false;
            };
            RegionTraverser.BreadthFirstTraverse(rootRegion, entryCondition, regionProcessor);
            return foundReg;
        }

        public static Region ClosestOutdoorRegionToPawn(Pawn pawn)
        {
            Region rootRegion = pawn.Position.GetRegion(pawn.MapHeld);
            if (rootRegion == null)
            {
                return null;
            }
            return ClosestOutdoorRegion(rootRegion, TraverseParms.For(pawn), pawn);
        }
    }
}
