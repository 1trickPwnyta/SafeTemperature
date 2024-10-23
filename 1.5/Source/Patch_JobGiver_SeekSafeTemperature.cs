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
            if (pawn.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Hidden) && !pawn.InMentalState)
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
                    Tuple<Map, Area> allowedArea = new Tuple<Map, Area>(pawn.MapHeld, pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap);

                    if (hypothermia != null && (heatstroke == null || hypothermia.Severity > heatstroke.Severity))
                    {
                        // Check if we have already calculated the best suboptimal region for warmth for the pawn's allowed area
                        if (BestSuboptimalRegionForWarmth.ContainsKey(allowedArea))
                        {
                            Tuple<Region, int> best = BestSuboptimalRegionForWarmth[allowedArea];
                            if (Find.TickManager.TicksGame < best.Item2 && TemperatureUtility.IsValidRegionForPawn(best.Item1, pawn))
                            {
                                safestRegion = best.Item1;
                            }
                        }
                        if (safestRegion == null)
                        {
                            // Look for the coldest region warmer than comfortable range
                            safestRegion = TemperatureUtility.ClosestRegionToTemperatureRange(pawn.Position, pawn.MapHeld, pawn, tempRange, TraverseParms.For(pawn), true);
                            // If no region exists, look for the warmest region
                            if (safestRegion == null)
                            {
                                FloatRange max = new FloatRange(float.MaxValue);
                                safestRegion = TemperatureUtility.ClosestRegionToTemperatureRange(pawn.Position, pawn.MapHeld, pawn, max, TraverseParms.For(pawn), false);
                            }
                            // Cache the region for other pawns with the same allowed area
                            BestSuboptimalRegionForWarmth[allowedArea] = new Tuple<Region, int>(safestRegion, Find.TickManager.TicksGame + 300);
                        }
                    }
                    if (heatstroke != null && (hypothermia == null || heatstroke.Severity > hypothermia.Severity))
                    {
                        // Check if we have already calculated the best suboptimal region for cooling for the pawn's allowed area
                        if (BestSuboptimalRegionForCooling.ContainsKey(allowedArea))
                        {
                            Tuple<Region, int> best = BestSuboptimalRegionForCooling[allowedArea];
                            if (Find.TickManager.TicksGame < best.Item2 && TemperatureUtility.IsValidRegionForPawn(best.Item1, pawn))
                            {
                                safestRegion = best.Item1;
                            }
                        }
                        if (safestRegion == null)
                        {
                            // Look for the warmest region colder than comfortable range
                            safestRegion = TemperatureUtility.ClosestRegionToTemperatureRange(pawn.Position, pawn.MapHeld, pawn, tempRange, TraverseParms.For(pawn), false);
                            // If no region exists, look for the coldest region
                            if (safestRegion == null)
                            {
                                FloatRange min = new FloatRange(float.MinValue);
                                safestRegion = TemperatureUtility.ClosestRegionToTemperatureRange(pawn.Position, pawn.MapHeld, pawn, min, TraverseParms.For(pawn), true);
                            }
                            // Cache the region for other pawns with the same allowed area
                            BestSuboptimalRegionForCooling[allowedArea] = new Tuple<Region, int>(safestRegion, Find.TickManager.TicksGame + 300);
                        }
                    }
                }

                if (safestRegion != null)
                {
                    bool positionInSafestRegion = safestRegion.Cells.ToList().Contains(pawn.Position);
                    IntVec3 pawnDestination = pawn.pather.curPath?.NodesReversed.First() ?? IntVec3.Invalid;
                    bool destInSafestRegion = pawnDestination.IsValid && safestRegion.Cells.ToList().Contains(pawnDestination);
                    if (positionInSafestRegion && pawnDestination.IsValid && !destInSafestRegion)
                    {
                        // If initial temperature injury and already in the safest region, wait there
                        if (pawn.HasMinTemperatureInjury())
                        {
                            __result = JobMaker.MakeJob(JobDefOf.Wait_SafeTemperature, 500, true);
                        }
                    }
                    if (!positionInSafestRegion && (!pawnDestination.IsValid || !destInSafestRegion))
                    {
                        // If serious temperature injury and not already in the safest region, go there
                        if (pawn.HasMaxTemperatureInjury())
                        {
                            JobGiver_SeekSafeTemperature.TryGetAllowedCellInRegion(safestRegion, pawn, out IntVec3 c);
                            __result = JobMaker.MakeJob(JobDefOf.GotoSafeTemperature, c);
                        }
                    }
                }
            }

            return false;
        }
    }
}
