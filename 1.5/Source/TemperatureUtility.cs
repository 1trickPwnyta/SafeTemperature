using Verse;

namespace SafeTemperature
{
    public static class TemperatureUtility
    {
        public static Region ColdestRegionWarmerThanTemperatureRange(IntVec3 root, Map map, Pawn pawn, FloatRange tempRange, TraverseParms traverseParms)
        {
            Region rootRegion = root.GetRegion(map);
            if (rootRegion == null)
            {
                return null;
            }

            float coldestTemp = float.MaxValue;
            foreach (Region region in map.regionGrid.AllRegions)
            {
                if (!region.IsDoorway && region.Room.Temperature < coldestTemp && region.Room.Temperature > tempRange.max)
                {
                    coldestTemp = region.Room.Temperature;
                }
            }

            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                IntVec3 intVec;
                if (!RimWorld.JobGiver_SeekSafeTemperature.TryGetAllowedCellInRegion(r, pawn, out intVec))
                {
                    return false;
                }
                if (r.Room.Temperature == coldestTemp)
                {
                    foundReg = r;
                    return true;
                }
                return false;
            };
            RegionTraverser.BreadthFirstTraverse(rootRegion, entryCondition, regionProcessor);
            return foundReg;
        }

        public static Region WarmestRegionColderThanTemperatureRange(IntVec3 root, Map map, Pawn pawn, FloatRange tempRange, TraverseParms traverseParms)
        {
            Region rootRegion = root.GetRegion(map);
            if (rootRegion == null)
            {
                return null;
            }

            float warmestTemp = float.MinValue;
            foreach (Region region in map.regionGrid.AllRegions)
            {
                if (!region.IsDoorway && region.Room.Temperature > warmestTemp && region.Room.Temperature < tempRange.min)
                {
                    warmestTemp = region.Room.Temperature;
                }
            }

            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
            Region foundReg = null;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                IntVec3 intVec;
                if (!RimWorld.JobGiver_SeekSafeTemperature.TryGetAllowedCellInRegion(r, pawn, out intVec))
                {
                    return false;
                }
                if (r.Room.Temperature == warmestTemp)
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
