using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsuRim.Armoury;

public class JobGiver_StanceBreach : ThinkNode_JobGiver
{
	private readonly JobGiver_AIBreaching inner = new JobGiver_AIBreaching();

	public override Job TryGiveJob(Pawn pawn)
	{
		Job job = FleshmassBreachUtility.TryGiveJob(pawn);
		if (job != null)
		{
			return job;
		}
		if (pawn?.mindState?.duty != null && pawn.GetLord()?.LordJob is LordJob_PlayerBreach)
		{
			return ((ThinkNode_JobGiver)inner).TryGiveJob(pawn);
		}
		return null;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		return base.DeepCopy(resolve);
	}
}
