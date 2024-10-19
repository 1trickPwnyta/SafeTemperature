using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

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

        public static Region ColdestRegionWarmerThanTemperatureRange(IntVec3 root, Map map, Pawn pawn, FloatRange tempRange, TraverseParms traverseParms)
        {
            Region rootRegion = root.GetRegion(map);
            if (rootRegion == null)
            {
                return null;
            }

            IEnumerable<Region> validRegions = map.regionGrid.AllRegions.Where(r => IsValidRegionForPawn(r, pawn));

            float coldestTemp = float.MaxValue;
            foreach (Region region in validRegions)
            {
                if (region.Room.Temperature < coldestTemp && region.Room.Temperature > tempRange.max)
                {
                    coldestTemp = region.Room.Temperature;
                }
            }

            if (!validRegions.Any(r => r.Room.Temperature != coldestTemp))
            {
                return null;
            }

            return ClosestRegionInTemperature(rootRegion, coldestTemp, traverseParms, pawn);
        }

        public static Region WarmestRegionColderThanTemperatureRange(IntVec3 root, Map map, Pawn pawn, FloatRange tempRange, TraverseParms traverseParms)
        {
            Region rootRegion = root.GetRegion(map);
            if (rootRegion == null)
            {
                return null;
            }

            IEnumerable<Region> validRegions = map.regionGrid.AllRegions.Where(r => IsValidRegionForPawn(r, pawn));

            float warmestTemp = float.MinValue;
            foreach (Region region in validRegions)
            {
                if (region.Room.Temperature > warmestTemp && region.Room.Temperature < tempRange.min)
                {
                    warmestTemp = region.Room.Temperature;
                }
            }

            if (!validRegions.Any(r => r.Room.Temperature != warmestTemp))
            {
                return null;
            }

            return ClosestRegionInTemperature(rootRegion, warmestTemp, traverseParms, pawn);
        }

        private static bool IsValidRegionForPawn(Region region, Pawn pawn)
        {
            return !region.IsDoorway && RimWorld.JobGiver_SeekSafeTemperature.TryGetAllowedCellInRegion(region, pawn, out IntVec3 intVec) && pawn.CanReach(new LocalTargetInfo(intVec), PathEndMode.OnCell, Danger.Deadly);
        }

        private static Region ClosestRegionInTemperature(Region rootRegion, float temperature, TraverseParms traverseParms, Pawn pawn)
        {
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (IsValidRegionForPawn(r, pawn) && r.Room.Temperature == temperature)
                {
                    foundReg = r;
                    return true;
                }
                return false;
            };
            RegionTraverser.BreadthFirstTraverse(rootRegion, entryCondition, regionProcessor);
            return foundReg;
        }
    }
}
