using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

internal static class PostedCombat
{
	private const int ScanInterval = 60;

	internal static void Check(Pawn pawn, int delta)
	{
		if (pawn?.Map == null || pawn.Dead || pawn.Downed || !pawn.IsHashIntervalTick(60, delta) || pawn.WorkTagIsDisabled(WorkTags.Violent))
		{
			return;
		}
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal != null && (pawn.CurJobDef == ASA_PostJobDefOf.ASA_ManPost || pawn.CurJobDef == ASA_PostJobDefOf.ASA_Patrol) && ThreatDetection.InRange(pawn, out var _))
		{
			if (pawn.drafter != null && !pawn.Drafted)
			{
				pawn.drafter.Drafted = true;
				compArsenal.autoDrafted = true;
			}
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
		}
	}
}
