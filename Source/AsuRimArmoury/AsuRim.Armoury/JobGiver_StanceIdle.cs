using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_StanceIdle : ThinkNode_JobGiver
{
	private const int ExpiryTicks = 90;

	public override Job TryGiveJob(Pawn pawn)
	{
		if (pawn == null || !pawn.Drafted)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat, pawn.Position);
		job.expiryInterval = 90;
		job.checkOverrideOnExpire = true;
		return job;
	}
}
