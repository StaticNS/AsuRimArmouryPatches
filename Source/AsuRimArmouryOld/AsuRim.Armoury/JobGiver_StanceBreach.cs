using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AsuRim.Armoury;

public class JobGiver_StanceBreach : ThinkNode_JobGiver
{
	private sealed class BreachingJobGiver : JobGiver_AIBreaching
	{
		public Job GiveJob(Pawn pawn)
		{
			return TryGiveJob(pawn);
		}
	}

	private readonly BreachingJobGiver inner = new BreachingJobGiver();

	protected override Job TryGiveJob(Pawn pawn)
	{
		Job job = FleshmassBreachUtility.TryGiveJob(pawn);
		if (job != null)
		{
			return job;
		}
		if (pawn?.mindState?.duty != null && pawn.GetLord()?.LordJob is LordJob_PlayerBreach)
		{
			return inner.GiveJob(pawn);
		}
		return null;
	}

	public override ThinkNode DeepCopy(bool resolve = true)
	{
		return base.DeepCopy(resolve);
	}
}
