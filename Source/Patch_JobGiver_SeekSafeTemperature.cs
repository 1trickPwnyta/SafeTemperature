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
            FloatRange tempRange = pawn.ComfortableTemperatureRange();
            if (pawn.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Initial))
            {
                if (!pawn.CurJob.GetTarget(TargetIndex.A).IsValid || !tempRange.Includes(pawn.CurJob.GetTarget(TargetIndex.A).Cell.GetTemperature(pawn.Map)))
                {
                    if (tempRange.Includes(pawn.AmbientTemperature))
                    {
                        __result = JobMaker.MakeJob(JobDefOf.Wait_SafeTemperature, 500, true);
                    }
                }
            }
        }
    }
}
