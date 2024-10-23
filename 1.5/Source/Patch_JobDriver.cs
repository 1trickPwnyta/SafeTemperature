using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SafeTemperature
{
    [HarmonyPatch(typeof(JobDriver))]
    [HarmonyPatch(nameof(JobDriver.DriverTick))]
    public static class Patch_JobDriver_DriverTick
    {
        public static void Postfix(Pawn ___pawn)
        {
            if (SafeTemperatureSettings.SeekSafeTemperatureIntervalEnabled && ___pawn.IsHashIntervalTick(SafeTemperatureSettings.SeekSafeTemperatureIntervalTicks) && ___pawn.IsFreeColonist && !___pawn.Downed && !___pawn.Drafted && !___pawn.CurJob.playerForced)
            {
                ThinkResult thinkResult = new JobGiver_SeekSafeTemperature().TryIssueJobPackage(___pawn, new JobIssueParams());
                if (thinkResult.Job != null)
                {
                    ___pawn.jobs.StartJob(thinkResult.Job, JobCondition.InterruptForced);
                }
            }
        }
    }
}
