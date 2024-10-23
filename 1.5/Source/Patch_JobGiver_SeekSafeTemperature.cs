using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace SafeTemperature
{
    [HarmonyPatch(typeof(JobGiver_SeekSafeTemperature))]
    [HarmonyPatch("TryGiveJob")]
    public static class Patch_JobGiver_SeekSafeTemperature_TryGiveJob
    {
        // int specifies the tick at which the cached value should expire
        private static Dictionary<Tuple<Map, Area>, Tuple<Region, int>> BestSuboptimalRegionForWarmth = new Dictionary<Tuple<Map, Area>, Tuple<Region, int>>();
        private static Dictionary<Tuple<Map, Area>, Tuple<Region, int>> BestSuboptimalRegionForCooling = new Dictionary<Tuple<Map, Area>, Tuple<Region, int>>();

        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            // Make sure pawn has at least initial temperature injury before making any calculations
            if (pawn.HasMinTemperatureInjury() && !pawn.InMentalState)
            {
                FloatRange tempRange = pawn.ComfortableTemperatureRange();

                // Find the safest region for the pawn, starting with the closest safe region
                Region safestRegion;
                safestRegion = JobGiver_SeekSafeTemperature.ClosestRegionWithinTemperatureRange(pawn.Position, pawn.MapHeld, pawn, tempRange, TraverseParms.For(pawn));

                // If no safe region exists, find the best suboptimal region based on whether the pawn is too cold or too hot
                if (SafeTemperatureSettings.UseSuboptimalTemperatureRegions && safestRegion == null)
                {
                    Hediff hypothermia = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia);
                    Hediff heatstroke = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);

                    if (hypothermia != null && (heatstroke == null || hypothermia.Severity > heatstroke.Severity))
                    {
                        // Check if we have already calculated the best suboptimal region for warmth for the pawn's allowed area
                        if (!BestSuboptimalRegionForWarmth.GetCachedRegion(pawn, out safestRegion))
                        {
                            // Look for the coldest region warmer than comfortable range
                            safestRegion = TemperatureUtility.ClosestRegionToTemperatureRange(pawn.Position, pawn.MapHeld, pawn, tempRange, TraverseParms.For(pawn), true);
                            // If no region exists, look for the warmest region
                            if (SafeTemperatureSettings.AllowUndercorrecting && safestRegion == null)
                            {
                                FloatRange max = new FloatRange(float.MaxValue);
                                safestRegion = TemperatureUtility.ClosestRegionToTemperatureRange(pawn.Position, pawn.MapHeld, pawn, max, TraverseParms.For(pawn), false);
                            }
                            // Cache the region for other pawns with the same allowed area
                            BestSuboptimalRegionForWarmth.CacheRegion(pawn, safestRegion);
                        }
                    }
                    if (heatstroke != null && (hypothermia == null || heatstroke.Severity > hypothermia.Severity))
                    {
                        // Check if we have already calculated the best suboptimal region for cooling for the pawn's allowed area
                        if (!BestSuboptimalRegionForCooling.GetCachedRegion(pawn, out safestRegion))
                        {
                            // Look for the warmest region colder than comfortable range
                            safestRegion = TemperatureUtility.ClosestRegionToTemperatureRange(pawn.Position, pawn.MapHeld, pawn, tempRange, TraverseParms.For(pawn), false);
                            // If no region exists, look for the coldest region
                            if (SafeTemperatureSettings.AllowUndercorrecting && safestRegion == null)
                            {
                                FloatRange min = new FloatRange(float.MinValue);
                                safestRegion = TemperatureUtility.ClosestRegionToTemperatureRange(pawn.Position, pawn.MapHeld, pawn, min, TraverseParms.For(pawn), true);
                            }
                            // Cache the region for other pawns with the same allowed area
                            BestSuboptimalRegionForCooling.CacheRegion(pawn, safestRegion);
                        }
                    }
                }

                if (safestRegion != null)
                {
                    float safestTemp = safestRegion.Room.Temperature;
                    FloatRange safestTempRange = new FloatRange(safestTemp - 2.77f, safestTemp + 2.77f);
                    bool positionInSafestTemp = safestTempRange.Includes(pawn.AmbientTemperature);
                    bool pathInSafestTemp = pawn.pather.curPath != null && pawn.pather.curPath.NodesReversed.All(n => safestTempRange.Includes(n.GetRegion(pawn.MapHeld).Room.Temperature));
                    bool destInSafestTemp = pawn.pather.curPath != null ? safestTempRange.Includes(pawn.pather.curPath.NodesReversed.First().GetRegion(pawn.MapHeld).Room.Temperature) : false;
                    int ticksToWaitInSafeTemp = SafeTemperatureSettings.SeekSafeTemperatureIntervalEnabled ? SafeTemperatureSettings.SeekSafeTemperatureIntervalTicks : 500;
                    if (positionInSafestTemp && pawn.pather.curPath != null && !pathInSafestTemp)
                    {
                        // If initial temperature injury and already in the safest region, wait there
                        if (pawn.HasMinTemperatureInjury())
                        {
                            __result = JobMaker.MakeJob(JobDefOf.Wait_SafeTemperature, ticksToWaitInSafeTemp, true);
                        }
                    }
                    if (!positionInSafestTemp && (pawn.pather.curPath == null || !destInSafestTemp))
                    {
                        // If serious temperature injury and not already in the safest region, go there
                        if (pawn.HasMaxTemperatureInjury())
                        {
                            JobGiver_SeekSafeTemperature.TryGetAllowedCellInRegion(safestRegion, pawn, out IntVec3 c);
                            __result = JobMaker.MakeJob(JobDefOf.GotoSafeTemperature, c);
                        }
                    }
                    if (__result == null && positionInSafestTemp && pawn.jobs.curJob != null && pawn.jobs.curJob.def == DefDatabase<JobDef>.GetNamed("Wait_SafeTemperature"))
                    {
                        // If no job was issued and already in the safest region, waiting in safe temperature, refresh the job
                        __result = JobMaker.MakeJob(JobDefOf.Wait_SafeTemperature, ticksToWaitInSafeTemp, true);
                    }
                }
            }

            return false;
        }

        private static void CacheRegion(this Dictionary<Tuple<Map, Area>, Tuple<Region, int>> cache, Pawn pawn, Region region)
        {
            Tuple<Map, Area> allowedArea = new Tuple<Map, Area>(pawn.MapHeld, pawn.playerSettings.AreaRestrictionInPawnCurrentMap);
            cache[allowedArea] = new Tuple<Region, int>(region, Find.TickManager.TicksGame + SafeTemperatureSettings.RegionCachingExpiryTicks);
        }

        private static bool GetCachedRegion(this Dictionary<Tuple<Map, Area>, Tuple<Region, int>> cache, Pawn pawn, out Region region)
        {
            region = null;
            Tuple<Map, Area> allowedArea = new Tuple<Map, Area>(pawn.MapHeld, pawn.playerSettings.AreaRestrictionInPawnCurrentMap);
            bool usedCache = false;
            if (cache.ContainsKey(allowedArea))
            {
                Tuple<Region, int> best = cache[allowedArea];
                if (Find.TickManager.TicksGame < best.Item2 && (best.Item1 == null || TemperatureUtility.IsValidRegionForPawn(best.Item1, pawn)))
                {
                    if (best.Item1 != null && best.Item1.Room.UsesOutdoorTemperature)
                    {
                        region = TemperatureUtility.ClosestOutdoorRegionToPawn(pawn);
                    }
                    else
                    {
                        region = best.Item1;
                    }
                    usedCache = true;
                }
            }
            return usedCache;
        }
    }
}
