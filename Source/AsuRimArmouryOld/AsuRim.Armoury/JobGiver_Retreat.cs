using RimWorld;
using Verse;
using Verse.AI;

namespace AsuRim.Armoury;

public class JobGiver_Retreat : ThinkNode_JobGiver
{
	protected override Job TryGiveJob(Pawn pawn)
	{
		CompArsenal compArsenal = CompArsenal.For(pawn);
		if (compArsenal?.policy == null)
		{
			return null;
		}
		float retreatAtHealth = compArsenal.policy.retreatAtHealth;
		if (retreatAtHealth <= 0f)
		{
			return null;
		}
		if (pawn.health.summaryHealth.SummaryHealthPercent > retreatAtHealth)
		{
			return null;
		}
		Area area = compArsenal.policy.RetreatAreaFor(pawn);
		if (area == null)
		{
			return null;
		}
		if (area[pawn.Position])
		{
			return null;
		}
		IntVec3 intVec = ClosestCellIn(area, pawn);
		if (!intVec.IsValid)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(JobDefOf.Goto, intVec);
		job.locomotionUrgency = LocomotionUrgency.Sprint;
		job.expiryInterval = 120;
		job.checkOverrideOnExpire = true;
		return job;
	}

	private static IntVec3 ClosestCellIn(Area area, Pawn pawn)
	{
		return AreaUtility.ClosestCellIn(area, pawn);
	}
}
