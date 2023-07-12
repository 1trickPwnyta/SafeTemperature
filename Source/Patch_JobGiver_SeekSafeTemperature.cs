using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SafeTemperature
{
    [HarmonyPatch(typeof(JobGiver_SeekSafeTemperature))]
    [HarmonyPatch("TryGiveJob")]
    public static class Patch_JobGiver_SeekSafeTemperature_TryGiveJob
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (pawn.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Initial))
            {
                FloatRange tempRange = pawn.ComfortableTemperatureRange();
                if (tempRange.Includes(pawn.AmbientTemperature))
                {
                    __result = JobMaker.MakeJob(JobDefOf.Wait_SafeTemperature, 500, true);
                }
            }
        }
    }
}
